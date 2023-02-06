using Newtonsoft.Json;
using System.Reflection;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace Chireiden.TShock.Omni;

[ApiVersion(2, 1)]
public partial class Plugin : TerrariaPlugin
{
    public override string Name => Assembly.GetExecutingAssembly().GetName().Name!;
    public override string Author => "SGKoishi";

    public string ConfigPath = Path.Combine(TShockAPI.TShock.SavePath, Consts.ConfigFile);

    public Config config;

    public Plugin(Main game) : base(game)
    {
        AppDomain.CurrentDomain.FirstChanceException += this.FirstChanceExceptionHandler;
        this.Order = int.MinValue;
        this.config = new Config();
        var bfany = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        this.Detour(
            nameof(this.Detour_UpdateCheckAsync),
            typeof(UpdateManager)
                .GetMethod(nameof(UpdateManager.UpdateCheckAsync), bfany)!,
            this.Detour_UpdateCheckAsync);
        this.Detour(
            nameof(this.Detour_HasPermission),
            typeof(TSPlayer)
                .GetMethod(nameof(TSPlayer.HasPermission), bfany, new[] { typeof(string) })!,
            this.Detour_HasPermission);
        this.Detour(
            nameof(this.Detour_PlayerActive),
            typeof(TSPlayer)
                .GetProperty(nameof(TSPlayer.Active), bfany)!
                .GetMethod!,
            this.Detour_PlayerActive);
        this.Detour(
            nameof(this.Detour_Lava_HitEffect),
            typeof(NPC)
                .GetMethod(nameof(NPC.HitEffect), bfany)!,
            this.Detour_Lava_HitEffect);
        this.Detour(
            nameof(this.Detour_Lava_KillTile),
            typeof(WorldGen)
                .GetMethod(nameof(WorldGen.KillTile), bfany)!,
            this.Detour_Lava_KillTile);
        this.Detour(
            nameof(this.Detour_Wildcard_GetPlayers),
            typeof(TSPlayer)
                .GetMethod(nameof(TSPlayer.FindByNameOrID), bfany)!,
            this.Detour_Wildcard_GetPlayers);
        this.Detour(
            nameof(this.Detour_Backport_2894),
            typeof(TShockAPI.DB.CharacterManager)
                .GetMethod(nameof(TShockAPI.DB.CharacterManager.InsertPlayerData), bfany)!,
            this.Detour_Backport_2894);
        this.Detour(
            nameof(this.Detour_Mitigation_SetTitle),
            typeof(TShockAPI.Utils)
                .GetMethod("SetConsoleTitle", bfany)!,
            this.Detour_Mitigation_SetTitle);
        this.Detour(
            nameof(this.Detour_Command_Alternative),
            typeof(TShockAPI.Commands)
                .GetMethod(nameof(TShockAPI.Commands.HandleCommand), bfany)!,
            this.Detour_Command_Alternative);
        this.ILHook(
            nameof(this.ILHook_Mitigation_DisabledInvincible),
            Utils.TShockType("Bouncer")
                .GetMethod("OnPlayerDamage", bfany)!,
            this.ILHook_Mitigation_DisabledInvincible);
    }

    private void OnReload(ReloadEventArgs? e)
    {
        var jss = new JsonSerializerSettings
        {
            ObjectCreationHandling = ObjectCreationHandling.Replace,
            Converters = new List<JsonConverter>
            {
                new Config.Limiter.LimiterConverter(),
            },
            Formatting = Formatting.Indented,
        };
        try
        {
            if (File.Exists(this.ConfigPath))
            {
                this.config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(this.ConfigPath), jss)!;
            }
        }
        catch (Exception ex)
        {
            e?.Player?.SendErrorMessage($"Failed to load config: {ex.Message}");
            return;
        }
        try
        {
            File.WriteAllText(this.ConfigPath, JsonConvert.SerializeObject(this.config, jss));
        }
        catch (Exception ex)
        {
            e?.Player?.SendErrorMessage($"Failed to save config: {ex.Message}");
            return;
        }
        e?.Player?.SendSuccessMessage("Chireiden.Omni loaded.");
        if (this.config.ShowConfig)
        {
            e?.Player?.SendInfoMessage(JsonConvert.SerializeObject(this.config, Formatting.Indented));
        }
        switch (this.config.Enhancements.TileProvider)
        {
            case Config.EnhancementsSettings.TileProviderOptions.CheckedGenericCollection:
                Terraria.Main.tile = Utils.CloneTileCollection(Terraria.Main.tile, new CheckedGenericCollection());
                break;
            case Config.EnhancementsSettings.TileProviderOptions.CheckedTypedCollection:
                Terraria.Main.tile = Utils.CloneTileCollection(Terraria.Main.tile, new CheckedTypedCollection());
                break;
            case Config.EnhancementsSettings.TileProviderOptions.AsIs:
            case Config.EnhancementsSettings.TileProviderOptions.Preset:
                break;
        }
        this.PermissionSetup();
        this.VanillaSetup();
        foreach (var command in Commands.ChatCommands)
        {
            Utils.TryRenameCommand(command, this.config.CommandRenames);
        }
        foreach (var command in Commands.TShockCommands)
        {
            Utils.TryRenameCommand(command, this.config.CommandRenames);
        }
        foreach (var command in this._hiddenCommands)
        {
            Utils.TryRenameCommand(command, this.config.CommandRenames);
        }
        var spamlim = this.config.Mitigation.ChatSpamRestrict;
        if (spamlim.Count > 0)
        {
            e?.Player?.SendInfoMessage("ChatSpam limit applied:");
            foreach (var limiter in spamlim)
            {
                e?.Player?.SendInfoMessage($"  {Math.Round(limiter.Maximum / limiter.RateLimit, 1):G} messages per {Math.Round(limiter.Maximum / 60, 1):G} seconds");
            }
        }
        var connlim = this.config.Mitigation.ConnectionLimit;
        if (connlim.Count > 0)
        {
            e?.Player?.SendInfoMessage("Connection limit applied:");
            foreach (var limiter in connlim)
            {
                e?.Player?.SendInfoMessage($"  {Math.Round(limiter.Maximum / limiter.RateLimit, 1):G} connections per IP per {Math.Round(limiter.Maximum, 1):G} seconds");
            }
        }
        foreach (var field in typeof(Consts.DataKey).GetFields())
        {
            if (field.GetValue(null) is string key)
            {
                foreach (var player in TShockAPI.TShock.Players)
                {
                    if (player is null)
                    {
                        continue;
                    }
                    player.SetData<object?>(key, null);
                }
            }
        }
        this._playerData.Clear();
        foreach (var hook in this._manipulators.Values)
        {
            hook.Undo();
            hook.Apply();
        }
        foreach (var command in this.config.StartupCommands)
        {
            TShockAPI.Commands.HandleCommand(TShockAPI.TSPlayer.Server, command);
        }
    }

    public override void Initialize()
    {
        On.Terraria.MessageBuffer.GetData += this.MMHook_PatchVersion_GetData;
        On.Terraria.GameContent.Tile_Entities.TEDisplayDoll.ctor += this.MMHook_MemoryTrim_DisplayDoll;
        On.Terraria.GameContent.Tile_Entities.TEHatRack.ctor += this.MMHook_MemoryTrim_HatRack;
        On.Terraria.NetMessage.SendData += this.MMHook_DebugPacket_SendData;
        On.Terraria.MessageBuffer.GetData += this.MMHook_DebugPacket_GetData;
        On.Terraria.Projectile.Kill += this.MMHook_Soundness_ProjectileKill;
        On.Terraria.WorldGen.clearWorld += this.MMHook_TileProvider_ClearWorld;
        On.Terraria.Netplay.OnConnectionAccepted += this.MMHook_Mitigation_OnConnectionAccepted;
        On.Terraria.Main.ReadLineInput += this.MMHook_CliConfig_ReadLine;
        On.Terraria.Localization.Language.GetTextValue_string += this.MMHook_CliConfig_LanguageText;
        OTAPI.Hooks.NetMessage.SendBytes += this.OTHook_Ghost_SendBytes;
        OTAPI.Hooks.NetMessage.SendBytes += this.OTHook_DebugPacket_SendBytes;
        OTAPI.Hooks.MessageBuffer.GetData += this.OTHook_Modded_GetData;
        OTAPI.Hooks.MessageBuffer.GetData += this.OTHook_Permission_SyncLoadout;
        OTAPI.Hooks.MessageBuffer.GetData += this.OTHook_Ping_GetData;
        OTAPI.Hooks.MessageBuffer.GetData += this.OTHook_Permission_SummonBoss;
        TerrariaApi.Server.ServerApi.Hooks.NetNameCollision.Register(this, this.TAHook_NameCollision);
        TerrariaApi.Server.ServerApi.Hooks.GamePostInitialize.Register(this, this.OnGamePostInitialize);
        TerrariaApi.Server.ServerApi.Hooks.GameUpdate.Register(this, this.TAHook_TimeoutInterval);
        TerrariaApi.Server.ServerApi.Hooks.GameUpdate.Register(this, this.TAHook_Mitigation_GameUpdate);
        TerrariaApi.Server.ServerApi.Hooks.ItemForceIntoChest.Register(this, this.TAHook_Permission_ItemForceIntoChest);
        TShockAPI.Hooks.PlayerHooks.PlayerCommand += this.TSHook_HideCommand_PlayerCommand;
        TShockAPI.Hooks.PlayerHooks.PlayerCommand += this.TSHook_Wildcard_PlayerCommand;
        TShockAPI.Hooks.PlayerHooks.PlayerPermission += this.TSHook_Sudo_OnPlayerPermission;
        TShockAPI.Hooks.GeneralHooks.ReloadEvent += this.OnReload;
        TShockAPI.TShock.Initialized += this.PostTShockInitialize;
        TShockAPI.GetDataHandlers.TogglePvp.Register(this.GDHook_Permission_TogglePvp);
        TShockAPI.GetDataHandlers.PlayerTeam.Register(this.GDHook_Permission_PlayerTeam);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            On.Terraria.MessageBuffer.GetData -= this.MMHook_PatchVersion_GetData;
            On.Terraria.GameContent.Tile_Entities.TEDisplayDoll.ctor -= this.MMHook_MemoryTrim_DisplayDoll;
            On.Terraria.GameContent.Tile_Entities.TEHatRack.ctor -= this.MMHook_MemoryTrim_HatRack;
            On.Terraria.NetMessage.SendData -= this.MMHook_DebugPacket_SendData;
            On.Terraria.MessageBuffer.GetData -= this.MMHook_DebugPacket_GetData;
            On.Terraria.NetMessage.SendData -= this.MMHook_DebugPacket_CatchSend;
            On.Terraria.MessageBuffer.GetData -= this.MMHook_DebugPacket_CatchGet;
            On.Terraria.Projectile.Kill -= this.MMHook_Soundness_ProjectileKill;
            On.Terraria.WorldGen.clearWorld -= this.MMHook_TileProvider_ClearWorld;
            On.Terraria.Netplay.OnConnectionAccepted -= this.MMHook_Mitigation_OnConnectionAccepted;
            On.Terraria.Main.ReadLineInput -= this.MMHook_CliConfig_ReadLine;
            On.Terraria.Localization.Language.GetTextValue_string -= this.MMHook_CliConfig_LanguageText;
            OTAPI.Hooks.NetMessage.SendBytes -= this.OTHook_Ghost_SendBytes;
            OTAPI.Hooks.NetMessage.SendBytes -= this.OTHook_DebugPacket_SendBytes;
            OTAPI.Hooks.MessageBuffer.GetData -= this.OTHook_Mitigation_GetData;
            OTAPI.Hooks.MessageBuffer.GetData -= this.OTHook_Permission_SyncLoadout;
            OTAPI.Hooks.MessageBuffer.GetData -= this.OTHook_Modded_GetData;
            OTAPI.Hooks.MessageBuffer.GetData -= this.OTHook_Ping_GetData;
            OTAPI.Hooks.MessageBuffer.GetData -= this.OTHook_Permission_SummonBoss;
            OTAPI.Hooks.Netplay.CreateTcpListener -= this.OTHook_Socket_OnCreate;
            TerrariaApi.Server.ServerApi.Hooks.NetNameCollision.Deregister(this, this.TAHook_NameCollision);
            TerrariaApi.Server.ServerApi.Hooks.GamePostInitialize.Deregister(this, this.OnGamePostInitialize);
            TerrariaApi.Server.ServerApi.Hooks.GameUpdate.Deregister(this, this.TAHook_TimeoutInterval);
            TerrariaApi.Server.ServerApi.Hooks.GameUpdate.Deregister(this, this.TAHook_Mitigation_GameUpdate);
            TShockAPI.Hooks.PlayerHooks.PlayerCommand -= this.TSHook_HideCommand_PlayerCommand;
            TShockAPI.Hooks.PlayerHooks.PlayerCommand -= this.TSHook_Wildcard_PlayerCommand;
            TShockAPI.Hooks.PlayerHooks.PlayerPermission -= this.TSHook_Sudo_OnPlayerPermission;
            TShockAPI.Hooks.GeneralHooks.ReloadEvent -= this.OnReload;
            TShockAPI.TShock.Initialized -= this.PostTShockInitialize;
            TShockAPI.GetDataHandlers.TogglePvp.UnRegister(this.GDHook_Permission_TogglePvp);
            TShockAPI.GetDataHandlers.PlayerTeam.UnRegister(this.GDHook_Permission_PlayerTeam);
            TShockAPI.GetDataHandlers.NPCAddBuff.UnRegister(this.GDHook_Mitigation_NpcAddBuff);
            var asm = Assembly.GetExecutingAssembly();
            Commands.ChatCommands.RemoveAll(c => c.CommandDelegate.Method?.DeclaringType?.Assembly == asm);
            foreach (var detour in this._detours.Values)
            {
                detour.Undo();
                detour.Dispose();
            }
            this._detours.Clear();
            foreach (var hook in this._manipulators.Values)
            {
                hook.Undo();
                hook.Dispose();
            }
            this._manipulators.Clear();
        }
        base.Dispose(disposing);
    }

    private void OnGamePostInitialize(EventArgs args)
    {
        OTAPI.Hooks.MessageBuffer.GetData += this.OTHook_Mitigation_GetData;
        On.Terraria.NetMessage.SendData += this.MMHook_DebugPacket_CatchSend;
        On.Terraria.MessageBuffer.GetData += this.MMHook_DebugPacket_CatchGet;
        TShockAPI.GetDataHandlers.NPCAddBuff.Register(this.GDHook_Mitigation_NpcAddBuff);
    }

    private void PostTShockInitialize()
    {
        OTAPI.Hooks.Netplay.CreateTcpListener += this.OTHook_Socket_OnCreate;
        Commands.ChatCommands.Add(new Command(Consts.Permissions.Whynot, this.Command_PermissionCheck, Consts.Commands.Whynot)
        {
            AllowServer = false,
        });
        Commands.ChatCommands.Add(new Command(Consts.Permissions.Admin.Ghost, this.Command_Ghost, Consts.Commands.Ghost)
        {
            AllowServer = false,
        });
        Commands.ChatCommands.Add(new Command(Consts.Permissions.Admin.SetLanguage, this.Command_Lang, Consts.Commands.SetLanguage));
        Commands.ChatCommands.Add(new Command(Consts.Permissions.PvPCommand, this.Command_PvP, Consts.Commands.SetPvp));
        Commands.ChatCommands.Add(new Command(Consts.Permissions.TeamCommand, this.Command_Team, Consts.Commands.SetTeam));
        Commands.ChatCommands.Add(new Command(new List<string> { Consts.Permissions.Admin.TriggerGarbageCollection, Permissions.maintenance },
            this.Command_GC, Consts.Commands.TriggerGarbageCollection));
        Commands.ChatCommands.Add(new Command(Consts.Permissions.Admin.DebugStat, this.Command_DebugStat, Consts.Commands.DebugStat));
        Commands.ChatCommands.Add(new Command(Consts.Permissions.Admin.MaxPlayers, this.Command_MaxPlayers, Consts.Commands.MaxPlayers));
        Commands.ChatCommands.Add(new Command(Consts.Permissions.Admin.TileProvider, this.Command_TileProvider, Consts.Commands.TileProvider));
        Commands.ChatCommands.Add(new Command(Consts.Permissions.TimeoutCommand, this.Command_SetTimeout, Consts.Commands.Timeout));
        Commands.ChatCommands.Add(new Command(Consts.Permissions.IntervalCommand, this.Command_SetInterval, Consts.Commands.Interval));
        Commands.ChatCommands.Add(new Command(Consts.Permissions.ClearInterval, this.Command_ClearInterval, Consts.Commands.ClearInterval));
        Commands.ChatCommands.Add(new Command(Consts.Permissions.ShowTimeout, this.Command_ListDelay, Consts.Commands.ShowTimeout));
        Commands.ChatCommands.Add(new Command(Consts.Permissions.Admin.RawBroadcast, this.Command_RawBroadcast, Consts.Commands.RawBroadcast));
        Commands.ChatCommands.Add(new Command(Consts.Permissions.Admin.Sudo, this.Command_Sudo, Consts.Commands.Sudo));
        Commands.ChatCommands.Add(new Command(Consts.Permissions.Admin.ListClients, this.Command_ListConnected, Consts.Commands.ListClients));
        Commands.ChatCommands.Add(new Command(Consts.Permissions.Admin.DumpBuffer, this.Command_DumpBuffer, Consts.Commands.DumpBuffer));
        Commands.ChatCommands.Add(new Command(Consts.Permissions.Admin.TerminateSocket, this.Command_TerminateSocket, Consts.Commands.TerminateSocket));
        Commands.ChatCommands.Add(new Command(Consts.Permissions.ResetCharacter, this.Command_ResetCharacter, Consts.Commands.ResetCharacter));
        Commands.ChatCommands.Add(new Command(Consts.Permissions.Ping, this.Command_Ping, Consts.Commands.Ping)
        {
            AllowServer = false,
        });
        Commands.ChatCommands.Add(new Command(new List<string> { Consts.Permissions.Chat, Permissions.canchat }, this.Command_Chat, Consts.Commands.Chat)
        {
            AllowServer = false,
        });
        Commands.ChatCommands.Add(new Command(Consts.Permissions.Admin.ExportCharacter, this.Command_ExportCharacter, Consts.Commands.ExportCharacter));
        this.OnReload(new ReloadEventArgs(TSPlayer.Server));
    }
}
