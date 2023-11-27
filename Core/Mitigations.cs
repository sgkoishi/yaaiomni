﻿using Mono.Cecil.Cil;
using MonoMod.Cil;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Net;
using System.Runtime.CompilerServices;

namespace Chireiden.TShock.Omni;

public partial class Plugin
{
    public class Detection
    {
        public delegate void SwapWhileUseEvent(int player, int slot);
        public event SwapWhileUseEvent? SwapWhileUse;
        internal void SwapWhileUseDetected(int player, int slot)
        {
            this.SwapWhileUse?.Invoke(player, slot);
        }
    }

    public Detection Detections = new Detection();

    private readonly bool[] _BuffUpdateNPC = new bool[Terraria.Main.npc.Length];

    private void OTHook_Mitigation_GetData(object? sender, OTAPI.Hooks.MessageBuffer.GetDataEventArgs args)
    {
        if (args.Result == OTAPI.HookResult.Cancel)
        {
            return;
        }

        var mitigation = this.config.Mitigation.Value;
        if (mitigation.DisableAllMitigation)
        {
            return;
        }

        switch (args.PacketId)
        {
            case (int) PacketTypes.PlayerInfo when mitigation.AllowCrossJourney:
            {
                // FIXME: Version specific, might be changed in the future
                if (Terraria.Main.GameModeInfo.IsJourneyMode == ((args.Instance.readBuffer[args.Length - 1] & 0b1000) == 0))
                {
                    args.Instance.readBuffer[args.Length - 1] ^= 0b1000;
                }
                break;
            }
            case (int) PacketTypes.PlayerSlot:
            {
                var index = args.Instance.whoAmI;
                var data = args.Instance.readBuffer.AsSpan(args.ReadOffset, args.Length - 1);

                if (mitigation.SwapWhileUsePE)
                {
                    var slot = BitConverter.ToInt16(data.Slice(1, 2));
                    if (Terraria.Main.player[index].controlUseItem && slot == Terraria.Main.player[index].selectedItem)
                    {
                        this.Statistics.MitigationRejectedSwapWhileUse++;
                        this.Detections.SwapWhileUseDetected(index, slot);
                        if (mitigation.SwapWhileUsePEHandleAttempt)
                        {
                            Terraria.Main.player[index].controlUseItem = false;
                            Terraria.NetMessage.TrySendData((int) PacketTypes.PlayerUpdate, -1, -1, null, index);
                        }
                    }
                }

                if (!mitigation.InventorySlotPE)
                {
                    break;
                }

                if (data.Length == 8 && data[0] == index)
                {
                    var slot = BitConverter.ToInt16(data.Slice(1, 2));
                    var stack = BitConverter.ToInt16(data.Slice(3, 2));
                    var prefix = data[5];
                    var type = BitConverter.ToInt16(data.Slice(6, 2));

                    var existingItem = Terraria.Main.player[index].GetInventory(slot);

                    if (existingItem == null)
                    {
                        this.Statistics.MitigationSlotPEAllowed++;
                        break;
                    }

                    if (!existingItem.IsAir || (type != 0 && stack != 0))
                    {
                        if (existingItem.netID != type || existingItem.stack != stack || existingItem.prefix != prefix)
                        {
                            this.Statistics.MitigationSlotPEAllowed++;
                            break;
                        }
                    }
                }

                args.CancelPacket();
                this.Statistics.MitigationSlotPE++;
                var player = TShockAPI.TShock.Players[index];
                if (player == null)
                {
                    break;
                }

                var value = this[player].DetectPE;
                this[player].DetectPE = value + 1;
                if (value % 500 == 0)
                {
                    var currentLoadoutIndex = Terraria.Main.player[index].CurrentLoadoutIndex;
                    Terraria.NetMessage.TrySendData((int) PacketTypes.SyncLoadout, -1, -1, null, index, (currentLoadoutIndex + 1) % 3);
                    Terraria.NetMessage.TrySendData((int) PacketTypes.SyncLoadout, -1, -1, null, index, currentLoadoutIndex);
                    this[player].IsPE = true;
                }
                break;
            }
            case (int) PacketTypes.PlayerSpawn:
            {
                // Spawn as dead with no respawn if dead in singleplayer?
                // Unable to repro with PC 1.4.4.9
                break;
            }
            case (int) PacketTypes.EffectHeal when mitigation.PotionSicknessPE:
            {
                var index = args.Instance.whoAmI;
                if (args.Instance.readBuffer[args.ReadOffset] != index)
                {
                    args.CancelPacket();
                    break;
                }
                if (Terraria.Main.player[index].inventory[Terraria.Main.player[index].selectedItem].potion)
                {
                    var amount = BitConverter.ToInt16(args.Instance.readBuffer.AsSpan(args.ReadOffset + 1, 2));
                    this[index].PendingRevertHeal = amount;
                }
                break;
            }
            case (int) PacketTypes.PlayerBuff when mitigation.PotionSicknessPE:
            {
                var index = args.Instance.whoAmI;
                if (args.Instance.readBuffer[args.ReadOffset] != index)
                {
                    args.CancelPacket();
                    break;
                }
                var buffcount = (args.Length - 1) / 2;
                for (var i = 0; i < buffcount; i++)
                {
                    var buff = BitConverter.ToInt16(args.Instance.readBuffer.AsSpan(args.ReadOffset + 1 + (i * 2), 2));
                    if (buff == Terraria.ID.BuffID.PotionSickness)
                    {
                        this[index].PendingRevertHeal = 0;
                    }
                }
                break;
            }
            case (int) PacketTypes.ClientSyncedInventory when mitigation.PotionSicknessPE:
            {
                var index = args.Instance.whoAmI;
                var pending = this[index].PendingRevertHeal;
                if (pending > 0)
                {
                    this.Statistics.MitigationRejectedSicknessHeal++;
                    this[index].PendingRevertHeal = 0;
                    Terraria.Main.player[index].statLife -= pending;
                    Terraria.NetMessage.TrySendData((int) PacketTypes.PlayerHp, -1, -1, null, index);
                }
                break;
            }
            case (int) PacketTypes.PlayerUpdate when mitigation.SwapWhileUsePE:
            {
                var index = args.Instance.whoAmI;
                if (args.Instance.readBuffer[args.ReadOffset] != index)
                {
                    args.CancelPacket();
                    break;
                }
                Terraria.BitsByte control = args.Instance.readBuffer[args.ReadOffset + 1];
                var selectedItem = args.Instance.readBuffer[args.ReadOffset + 5];
                if (Terraria.Main.player[index].controlUseItem && control[5] && selectedItem != Terraria.Main.player[index].selectedItem)
                {
                    this.Statistics.MitigationRejectedSwapWhileUse++;
                    if (mitigation.SwapWhileUsePEHandleAttempt)
                    {
                        args.CancelPacket();
                        Terraria.Main.player[index].controlUseItem = false;
                        Terraria.NetMessage.TrySendData((int) PacketTypes.PlayerUpdate, -1, -1, null, index);
                    }
                    break;
                }
                break;
            }
            case (int) PacketTypes.LoadNetModule:
            {
                var type = BitConverter.ToUInt16(args.Instance.readBuffer.AsSpan(args.ReadOffset, 2));
                if (type == Terraria.Net.NetManager.Instance.GetId<Terraria.GameContent.NetModules.NetTextModule>())
                {
                    var index = args.Instance.whoAmI;
                    var player = TShockAPI.TShock.Players[index];
                    if (player == null)
                    {
                        break;
                    }
                    foreach (var limiter in this[player].ChatSpamRestrict)
                    {
                        if (!limiter.Allowed)
                        {
                            this.Statistics.MitigationRejectedChat++;
                            args.CancelPacket();
                            break;
                        }
                    }
                }
                break;
            }
            case (int) PacketTypes.SyncExtraValue:
            {
                switch (mitigation.ExpertExtraCoin.Value)
                {
                    case Config.MitigationSettings.ExpertCoinHandler.AsIs:
                    {
                        break;
                    }
                    case Config.MitigationSettings.ExpertCoinHandler.DisableValue:
                    case Config.MitigationSettings.ExpertCoinHandler.ServerSide:
                    {
                        this.Statistics.MitigationCoinReduced += BitConverter.ToInt32(args.Instance.readBuffer.AsSpan(args.ReadOffset + 2, 4));
                        args.CancelPacket();
                        break;
                    }
                }
                break;
            }
            default:
            {
                break;
            }
        }
    }

    private void GDHook_Mitigation_NpcAddBuff(object? sender, TShockAPI.GetDataHandlers.NPCAddBuffEventArgs args)
    {
        if (args.Handled)
        {
            return;
        }

        var mitigation = this.config.Mitigation.Value;
        if (mitigation.DisableAllMitigation)
        {
            return;
        }

        if (!mitigation.NpcUpdateBuffRateLimit)
        {
            return;
        }

        this._BuffUpdateNPC[args.ID] = true;
        Terraria.Main.npc[args.ID].AddBuff(args.Type, args.Time, quiet: true);
        args.Handled = true;
    }

    private void TAHook_Mitigation_GameUpdate(EventArgs _)
    {
        var mitigation = this.config.Mitigation.Value;
        if (mitigation.DisableAllMitigation)
        {
            return;
        }

        if (mitigation.NpcUpdateBuffRateLimit)
        {
            if (this.UpdateCounter % 10 == 0)
            {
                for (var i = 0; i < Terraria.Main.npc.Length; i++)
                {
                    if (this._BuffUpdateNPC[i])
                    {
                        Terraria.NetMessage.TrySendData((int) PacketTypes.NpcUpdateBuff, -1, -1, null, i);
                        this._BuffUpdateNPC[i] = false;
                    }
                }
            }
        }

        if (Terraria.Main.expertMode)
        {
            switch (mitigation.ExpertExtraCoin.Value)
            {
                case Config.MitigationSettings.ExpertCoinHandler.AsIs:
                case Config.MitigationSettings.ExpertCoinHandler.DisableValue:
                {
                    break;
                }
                case Config.MitigationSettings.ExpertCoinHandler.ServerSide:
                {
                    foreach (var item in Terraria.Main.item)
                    {
                        if (item?.active != true || item.instanced || !item.IsACoin || item.timeLeftInWhichTheItemCannotBeTakenByEnemies != 0)
                        {
                            continue;
                        }
                        var weight = item.type switch
                        {
                            71 => 1,
                            72 => 100,
                            73 => 10000,
                            74 => 1000000,
                            _ => throw new SwitchExpressionException($"Unexpected coin {item.type}")
                        };
                        var stack = item.stack;
                        item.GetPickedUpByMonsters_Money(item.whoAmI);
                        if (item.stack != stack)
                        {
                            this.Statistics.MitigationCoinReduced -= (stack - item.stack) * weight;
                        }
                    }
                    break;
                }
            }
        }
    }

    private static readonly bool ShouldSuppressTitle = !System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)
        && !(Environment.GetEnvironmentVariable("TERM")?.Contains("xterm") ?? false);
    private void Detour_Mitigation_SetTitle(Action<TShockAPI.Utils, bool> orig, TShockAPI.Utils self, bool empty)
    {
        var mitigation = this.config.Mitigation.Value;
        if (!mitigation.DisableAllMitigation && mitigation.SuppressTitle)
        {
            if (ShouldSuppressTitle)
            {
                return;
            }
        }

        orig(self, empty);
    }

    internal class ConnectionStore
    {
        public ConcurrentDictionary<string, Connection> Connections { get; } = new ConcurrentDictionary<string, Connection>();
        public ConditionalWeakTable<Terraria.Net.Sockets.ISocket, Float64Object> ConnectTime { get; } = new ConditionalWeakTable<Terraria.Net.Sockets.ISocket, Float64Object>();

        internal class Connection
        {
            public required IPAddress Address { get; set; }
            public required ConcurrentBag<Limiter> Limit { init; get; }
        }

        public void PurgeCache()
        {
            var alive = this.ConnectTime
                .Select((kv) => kv.Key.GetRemoteAddress() is Terraria.Net.TcpAddress tcpa ? tcpa.Address : null)
                .Where((a) => a != null)
                .Select(a => a!)
                .ToArray();
            var tbr = this.Connections
                .Where((kv) => !alive.Any(a => a.Equals(kv.Value.Address)))
                .Select((kv) => kv.Key)
                .ToList();
            foreach (var k in tbr)
            {
                this.Connections.TryRemove(k, out _);
            }
        }
    }

    internal class Float64Object
    {
        public double Value;
    }

    private readonly ConnectionStore _connPool = new ConnectionStore();
    private void MMHook_Mitigation_OnConnectionAccepted(On.Terraria.Netplay.orig_OnConnectionAccepted orig, Terraria.Net.Sockets.ISocket client)
    {
        var mitigation = this.config.Mitigation.Value;
        var cl = mitigation.ConnectionLimit.Value;
        var nl = mitigation.LimitedNetwork.Value;

        if (mitigation.DisableAllMitigation
            || cl.Count == 0 || client.GetRemoteAddress() is not Terraria.Net.TcpAddress tcpa
            || nl is Config.MitigationSettings.NetworkLimit.None
            || (nl is Config.MitigationSettings.NetworkLimit.Public && Utils.PrivateIPv4Address(tcpa.Address)))
        {
            orig(client);
            return;
        }

        var addrs = tcpa.Address.ToString();
        var cd = this._connPool.Connections.GetOrAdd(addrs, _ => new ConnectionStore.Connection
        {
            Address = tcpa.Address,
            Limit = new ConcurrentBag<Limiter>(cl.Select(lc => (Limiter) lc)),
        });
        var time = new TimeSpan(DateTime.Now.Ticks).TotalSeconds;
        this._connPool.ConnectTime.Add(client, new Float64Object
        {
            Value = time,
        });
        foreach (var limiter in cd.Limit)
        {
            if (!limiter.Allowed)
            {
                Interlocked.Increment(ref this.Statistics.MitigationRejectedConnection);
                client.Close();
                TShockAPI.TShock.Log.ConsoleInfo($"Connection from {tcpa.Address} ({tcpa.Port}) rejected due to connection limit.");
                return;
            }
        }
        this.CheckConnectionTimeout();

        orig(client);
    }

    private void CheckConnectionTimeout()
    {
        var count = Terraria.Netplay.Clients.Count(rc => rc?.IsConnected() == true);

        if (count <= Terraria.Main.maxNetPlayers * 0.6)
        {
            return;
        }

        for (var i = 0; i < Terraria.Main.maxNetPlayers; i++)
        {
            if (Terraria.Netplay.Clients[i].IsConnected()
                && Terraria.Netplay.Clients[i].Socket.GetRemoteAddress() is Terraria.Net.TcpAddress tcpa)
            {
                if (!this._connPool.ConnectTime.TryGetValue(Terraria.Netplay.Clients[i].Socket, out var ct))
                {
                    throw new Exception("Connection time not found");
                }

                var time = new TimeSpan(DateTime.Now.Ticks).TotalSeconds;

                foreach (var (state, timeout) in this.config.Mitigation.Value.ConnectionStateTimeout.Value)
                {
                    var elapsed = time - ct.Value;
                    if (Terraria.Netplay.Clients[i].State == state && elapsed > timeout)
                    {
                        Interlocked.Increment(ref this.Statistics.MitigationTerminatedConnection);
                        Terraria.Netplay.Clients[i].Socket.Close();
                        TShockAPI.TShock.Log.ConsoleInfo($"Connection from {tcpa.Address} ({tcpa.Port}, state {state} for {Math.Round(time - ct.Value, 1):G}s) disconnected due to connection state timeout.");
                        break;
                    }
                }
            }
        }

        this._connPool.PurgeCache();
    }

    private void ILHook_Mitigation_DisabledInvincible(ILContext context)
    {
        var mitigation = this.config.Mitigation.Value;
        if (mitigation.DisableAllMitigation)
        {
            return;
        }

        var cursor = new ILCursor(context);
        try
        {
            cursor.GotoNext(MoveType.After, (i) => i.MatchCallvirt<TShockAPI.TSPlayer>(nameof(TShockAPI.TSPlayer.IsBeingDisabled)));
            switch (mitigation.DisabledDamageHandler.Value)
            {
                case Config.MitigationSettings.DisabledDamageAction.AsIs:
                case Config.MitigationSettings.DisabledDamageAction.Ghost:
                    break;
                case Config.MitigationSettings.DisabledDamageAction.Hurt:
                    cursor.Emit(OpCodes.Pop);
                    cursor.Emit(OpCodes.Ldc_I4_0);
                    break;
            }
        }
        catch
        {
            Utils.ShowError($"Attempt hook {nameof(Config.Mitigation)}.{nameof(Config.MitigationSettings.DisabledDamageHandler)} failed, might be already fixed.");
        }
    }

    private void ILHook_Mitigation_KeepRestAlive(ILContext context)
    {
        // FIXME: This is a backport of Pryaxis/TShock#2925
        try
        {
            var mitigation = this.config.Mitigation.Value;
            if (!mitigation.DisableAllMitigation && mitigation.KeepRestAlive)
            {
                var cursor = new ILCursor(context);
                cursor.GotoNext(MoveType.Before, (i) => i.MatchCallvirt("HttpServer.Headers.ConnectionHeader", "set_Type"));
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldc_I4_1);
            }
        }
        catch
        {
            Utils.ShowError($"Attempt hook {nameof(Config.Mitigation)}.{nameof(Config.MitigationSettings.KeepRestAlive)} failed, might be already fixed.");
        }
    }

    private void MMHook_Mitigation_I18nCommand(On.Terraria.Initializers.ChatInitializer.orig_Load orig)
    {
        // Pryaxis/TShock#2914
        Terraria.UI.Chat.ChatManager.Commands._localizedCommands.Clear();
        orig();
        var mitigation = this.config.Mitigation.Value;
        if (!mitigation.DisableAllMitigation && mitigation.UseEnglishCommand)
        {
            var currentLanguage = Terraria.Localization.LanguageManager.Instance.ActiveCulture;
            Terraria.Localization.LanguageManager.Instance.LoadLanguage(Terraria.Localization.GameCulture.FromCultureName(Terraria.Localization.GameCulture.CultureName.English));
            var items = Terraria.UI.Chat.ChatManager.Commands._localizedCommands.ToList();
            Terraria.UI.Chat.ChatManager.Commands._localizedCommands.Clear();
            foreach (var (key, value) in items)
            {
                Terraria.UI.Chat.ChatManager.Commands._localizedCommands[new Terraria.Localization.LocalizedText(key.Key, key.Value)] = value;
            }
            Terraria.Localization.LanguageManager.Instance.LoadLanguage(currentLanguage);
        }
    }

    private delegate JObject ConfigUpdateAction(JObject cfg, out bool requiredUpgrade);
    private JObject Detour_Mitigation_ConfigUpdate(ConfigUpdateAction orig, JObject cfg, out bool requiredUpgrade)
    {
        var result = orig(cfg, out requiredUpgrade);
        var mitigation = this.config.Mitigation.Value;
        if (!mitigation.DisableAllMitigation && result.Children().Count() > 1)
        {
            switch (mitigation.AcceptPartialUpdatedConfig.Value)
            {
                case Config.MitigationSettings.PartialConfigAction.Ignore:
                {
                    break;
                }
                case Config.MitigationSettings.PartialConfigAction.Replace:
                {
                    if (result.SelectToken("Settings") is not JObject root)
                    {
                        break;
                    }

                    foreach (var field in result.Children())
                    {
                        if (field is not JProperty jp || jp.Name == "Settings")
                        {
                            continue;
                        }

                        if (!root.TryAdd(jp.Name, jp.Value))
                        {
                            root[jp.Name] = jp.Value;
                        }

                        Console.WriteLine($"Set field \"{jp.Name}\" to \"{jp.Value}\".");
                    }
                    break;
                }
                default:
                {
                    throw new SwitchExpressionException($"Unexpected AcceptPartialUpdatedConfig {mitigation.AcceptPartialUpdatedConfig.Value}");
                }
            }
        }
        return result;
    }

    private void MMHook_Mitigation_DoorDropItem(On.Terraria.WorldGen.orig_DropDoorItem orig, int x, int y, int doorStyle)
    {
        var mitigation = this.config.Mitigation.Value;
        if (mitigation.DisableAllMitigation)
        {
            return;
        }

        if (doorStyle > TShockAPI.GetDataHandlers.MaxPlaceStyles[Terraria.ID.TileID.ClosedDoor])
        {
            // Can be used to spawn arbitrary items
            return;
        }

        orig(x, y, doorStyle);
    }

    private void MMHook_Mitigation_TileDropItem(On.Terraria.WorldGen.orig_KillTile_GetItemDrops orig, int x, int y, Terraria.ITile tileCache, out int dropItem, out int dropItemStack, out int secondaryItem, out int secondaryItemStack, bool includeLargeObjectDrops)
    {
        var mitigation = this.config.Mitigation.Value;
        if (mitigation.DisableAllMitigation || !mitigation.OverflowWorldGenItemID)
        {
            orig(x, y, tileCache, out dropItem, out dropItemStack, out secondaryItem, out secondaryItemStack, includeLargeObjectDrops);
            return;
        }

        dropItem = 0;
        dropItemStack = 0;
        secondaryItem = 0;
        secondaryItemStack = 0;

        if (tileCache.type == Terraria.ID.TileID.Torches)
        {
            var style = tileCache.frameY / 22;
            if (style > TShockAPI.GetDataHandlers.MaxPlaceStyles[Terraria.ID.TileID.Torches])
            {
                return;
            }
        }

        orig(x, y, tileCache, out dropItem, out dropItemStack, out secondaryItem, out secondaryItemStack, includeLargeObjectDrops);
    }

    private void MMHook_Mitigation_WorldGenNextCount(On.Terraria.WorldGen.orig_nextCount orig, int x, int y, bool jungle, bool lavaOk)
    {
        var mitigation = this.config.Mitigation.Value;
        if (mitigation.DisableAllMitigation || !mitigation.NonRecursiveWorldGenTileCount)
        {
            orig(x, y, jungle, lavaOk);
            return;
        }

        var pendingTiles = new Stack<(int, int)>();
        pendingTiles.Push((x, y));
        while (pendingTiles.Count > 0 && Terraria.WorldGen.numTileCount < Terraria.WorldGen.maxTileCount)
        {
            (x, y) = pendingTiles.Pop();
            if (x <= 1 || x >= Terraria.Main.maxTilesX - 1 || y <= 1 || y >= Terraria.Main.maxTilesY - 1
                || (Terraria.Main.tile[x, y].wall == Terraria.ID.WallID.LivingWoodUnsafe)
                || (Terraria.Main.tile[x, y].shimmer() && Terraria.Main.tile[x, y].liquid > 0))
            {
                break;
            }
            if (Terraria.WorldGen.CountedTiles.ContainsKey(new Microsoft.Xna.Framework.Point(x, y)))
            {
                continue;
            }
            if (!jungle)
            {
                if (Terraria.Main.tile[x, y].wall != 0)
                {
                    break;
                }
                if (Terraria.Main.tile[x, y].lava() && Terraria.Main.tile[x, y].liquid > 0)
                {
                    Terraria.WorldGen.lavaCount++;
                    if (!lavaOk)
                    {
                        break;
                    }
                }
            }
            if (Terraria.Main.tile[x, y].active())
            {
                if (Terraria.Main.tile[x, y].type == Terraria.ID.TileID.MushroomGrass)
                {
                    Terraria.WorldGen.shroomCount++;
                }
                if (Terraria.Main.tile[x, y].type == Terraria.ID.TileID.Stone)
                {
                    Terraria.WorldGen.rockCount++;
                }
                if (Terraria.Main.tile[x, y].type is Terraria.ID.TileID.SnowBlock or Terraria.ID.TileID.IceBlock)
                {
                    Terraria.WorldGen.iceCount++;
                }
                if (Terraria.Main.tile[x, y].type is Terraria.ID.TileID.Sand or Terraria.ID.TileID.Sandstone or Terraria.ID.TileID.HardenedSand)
                {
                    Terraria.WorldGen.sandCount++;
                }
            }
            if (!Terraria.WorldGen.SolidTile(x, y))
            {
                Terraria.WorldGen.CountedTiles.Add(new Microsoft.Xna.Framework.Point(x, y), true);
                Terraria.WorldGen.numTileCount++;
                // Reversed order for the stack
                pendingTiles.Push((x, y + 1));
                pendingTiles.Push((x, y - 1));
                pendingTiles.Push((x + 1, y));
                pendingTiles.Push((x - 1, y));
            }
        }
        Terraria.WorldGen.numTileCount = Terraria.WorldGen.maxTileCount;
    }
}