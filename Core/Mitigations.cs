using Mono.Cecil.Cil;
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

        public delegate void IndexMismatchEvent(int expectedIndex, int receivedIndex, PacketTypes type);
        public event IndexMismatchEvent? IndexMismatch;
        internal void IndexMismatchDetected(int expectedIndex, int receivedIndex, PacketTypes type)
        {
            this.IndexMismatch?.Invoke(expectedIndex, receivedIndex, type);
        }

        public delegate void PotionBypassEvent(int player, int slot);
        public event PotionBypassEvent? PotionBypass;
        internal void PotionBypassDetected(int player, int amount)
        {
            this.PotionBypass?.Invoke(player, amount);
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

                if (args.Instance.readBuffer[args.ReadOffset] != index)
                {
                    this.Statistics.IndexMismatch++;
                    this.Detections.IndexMismatchDetected(index, args.Instance.readBuffer[args.ReadOffset], PacketTypes.PlayerSlot);
                }

                var slot = args.Read<short>(1);
                var stack = args.Read<short>(3);
                var prefix = args.Instance.readBuffer[args.ReadOffset + 5];
                var type = args.Read<short>(6);

                if (mitigation.SwapWhileUsePE)
                {
                    var existingItem = Terraria.Main.player[index].GetInventory(slot);
                    if (Terraria.Main.player[index].controlUseItem && slot == Terraria.Main.player[index].selectedItem
                        && type != existingItem.netID && (type != 0 || stack != 0))
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

                if (args.Length == 9)
                {
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

                var value = this[player]!.DetectPE;
                this[player]!.DetectPE = value + 1;
                if (value % 1000 == 0)
                {
                    var currentLoadoutIndex = Terraria.Main.player[index].CurrentLoadoutIndex;
                    Terraria.NetMessage.TrySendData((int) PacketTypes.SyncLoadout, -1, -1, null, index, (currentLoadoutIndex + 1) % 3);
                    Terraria.NetMessage.TrySendData((int) PacketTypes.SyncLoadout, -1, -1, null, index, currentLoadoutIndex);
                    this[player]!.IsPE = true;
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
                if (Terraria.Main.player[index].inventory[Terraria.Main.player[index].selectedItem].potion && Terraria.Main.player[index].talkNPC != -1)
                {
                    var amount = args.Read<short>(1);
                    this[index]!.PendingRevertHeal = Math.Min(amount, Terraria.Main.player[index].statLifeMax2 - Terraria.Main.player[index].statLife);
                }
                break;
            }
            case (int) PacketTypes.ClientSyncedInventory when mitigation.PotionSicknessPE:
            {
                var index = args.Instance.whoAmI;
                var pending = this[index]!.PendingRevertHeal;
                if (pending > 0)
                {
                    this.Statistics.MitigationRejectedSicknessHeal++;
                    this[index]!.PendingRevertHeal = 0;
                    Terraria.Main.player[index].statLife -= pending;
                    Terraria.NetMessage.TrySendData((int) PacketTypes.PlayerHp, -1, -1, null, index);
                    this.Detections.PotionBypassDetected(index, pending);
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
                var type = args.Read<short>(0);
                if (type == Terraria.Net.NetManager.Instance.GetId<Terraria.GameContent.NetModules.NetTextModule>())
                {
                    var index = args.Instance.whoAmI;
                    var player = TShockAPI.TShock.Players[index];
                    if (player == null)
                    {
                        break;
                    }
                    foreach (var limiter in this[player]!.ChatSpamRestrict)
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
                        this.Statistics.MitigationCoinReduced += args.Read<int>(2);
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

        if (mitigation.RecursiveTileBreak)
        {
            var kt = 0;
            var count = this._pendingKilled.Count;
            while (kt < this._pendingKilled.Count)
            {
                var item = this._pendingKilled[kt];
                var ti = (int) (item >> 32);
                var tj = (int) item;
                Terraria.WorldGen.SquareTileFrame(ti, tj);
                kt++;
            }

            this.Statistics.MitigationNewTileKillTriggered += this._pendingKilled.Count - count;
            this._pendingKilled.Clear();
            this._pendingTileFrame.Clear();
        }
    }

    private static readonly bool ShouldSuppressTitle = !System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)
        && !(Environment.GetEnvironmentVariable("TERM")?.Contains("xterm") ?? false);
    private void Detour_Mitigation_SetTitle(Action<TShockAPI.Utils, bool> orig, TShockAPI.Utils self, bool empty)
    {
        var mitigation = this.config.Mitigation.Value;
        if (!mitigation.DisableAllMitigation)
        {
            switch (mitigation.SuppressTitle.Value)
            {
                case Config.MitigationSettings.TitleSuppression.Enabled:
                case Config.MitigationSettings.TitleSuppression.Smart when ShouldSuppressTitle:
                {
                    return;
                }
            }
        }

        orig(self, empty);
    }

    private void ILHook_Mitigation_DisabledInvincible(ILContext context)
    {
        var mitigation = this.config.Mitigation.Value;
        if (mitigation.DisableAllMitigation)
        {
            return;
        }

        try
        {
            var cursor = new ILCursor(context);
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
            TShockAPI.TShock.Log.ConsoleInfo($"Attempt to spawn item using glitched door style: {doorStyle}");
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
                TShockAPI.TShock.Log.ConsoleInfo($"Attempt to spawn item using glitched torch style: {style}");
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

    private void GDHook_Mitigation_PlayerBuffUpdate(object? sender, TShockAPI.GetDataHandlers.PlayerBuffUpdateEventArgs args)
    {
        var currentPosition = args.Data.Position;
        for (var i = 0; i < Terraria.Player.maxBuffs; i++)
        {
            var buff = System.IO.Streams.StreamExt.ReadUInt16(args.Data);
            if (buff == Terraria.ID.BuffID.PotionSickness)
            {
                this[args.Player]!.PendingRevertHeal = 0;
            }
        }
        args.Data.Position = currentPosition;
    }

    private bool Detour_Mitigation_HandleSyncLoadout(Func<TShockAPI.GetDataHandlerArgs, bool> orig, TShockAPI.GetDataHandlerArgs args)
    {
        var mitigation = this.config.Mitigation.Value;
        return (mitigation.DisableAllMitigation || !mitigation.LoadoutSwitchWithoutSSC) && orig(args);
    }
}