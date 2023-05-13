using Newtonsoft.Json;
using System.Reflection;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.DB;
using TShockAPI.Hooks;

namespace Chireiden.TShock.Omni;

[ApiVersion(2, 1)]
public partial class Plugin : TerrariaPlugin
{
    public override string Name => $"{Assembly.GetExecutingAssembly().GetName().Name} {CommitHashAttribute.GetCommitHash()}";
    public override string Author => "SGKoishi";
    public override Version Version => Assembly.GetExecutingAssembly().GetName().Version!;
    public override string Description => Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description!;
    public override string UpdateURL => Assembly.GetExecutingAssembly()
        .GetCustomAttributes<AssemblyMetadataAttribute>()
        .FirstOrDefault(x => x.Key == "RepositoryUrl")?.Value!;

    public string ConfigPath = Path.Combine(TShockAPI.TShock.SavePath, DefinedConsts.Misc.ConfigFile);
    private const BindingFlags _bfany = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    public Config config;

    public Plugin(Main game) : base(game)
    {
        AppDomain.CurrentDomain.FirstChanceException += this.FirstChanceExceptionHandler;
        this.Order = int.MinValue;
        this.config = new Config();
        this.LoadConfig(Utils.ConsolePlayer.Instance);
        this.config._init = false;
        this.Detour(
            nameof(this.Detour_UpdateCheckAsync),
            typeof(UpdateManager)
                .GetMethod(nameof(UpdateManager.UpdateCheckAsync), _bfany)!,
            this.Detour_UpdateCheckAsync);
        this.Detour(
            nameof(this.Detour_HasPermission),
            typeof(TSPlayer)
                .GetMethod(nameof(TSPlayer.HasPermission), _bfany, new[] { typeof(string) })!,
            this.Detour_HasPermission);
        this.Detour(
            nameof(this.Detour_PlayerActive),
            typeof(TSPlayer)
                .GetProperty(nameof(TSPlayer.Active), _bfany)!
                .GetMethod!,
            this.Detour_PlayerActive);
        this.Detour(
            nameof(this.Detour_Lava_HitEffect),
            typeof(NPC)
                .GetMethod(nameof(NPC.HitEffect), _bfany)!,
            this.Detour_Lava_HitEffect);
        this.Detour(
            nameof(this.Detour_Lava_KillTile),
            typeof(WorldGen)
                .GetMethod(nameof(WorldGen.KillTile), _bfany)!,
            this.Detour_Lava_KillTile);
        this.Detour(
            nameof(this.Detour_Wildcard_GetPlayers),
            typeof(TSPlayer)
                .GetMethod(nameof(TSPlayer.FindByNameOrID), _bfany)!,
            this.Detour_Wildcard_GetPlayers);
        this.Detour(
            nameof(this.Detour_Mitigation_SetTitle),
            typeof(TShockAPI.Utils)
                .GetMethod("SetConsoleTitle", _bfany)!,
            this.Detour_Mitigation_SetTitle);
        this.Detour(
            nameof(this.Detour_Command_Alternative),
            typeof(TShockAPI.Commands)
                .GetMethod(nameof(TShockAPI.Commands.HandleCommand), _bfany)!,
            this.Detour_Command_Alternative);
        this.Detour(
            nameof(this.Detour_Mitigation_I18nCommand),
            typeof(Terraria.Initializers.ChatInitializer)
                .GetMethod(nameof(Terraria.Initializers.ChatInitializer.Load), _bfany)!,
            this.Detour_Mitigation_I18nCommand);
        this.Detour(
            nameof(this.Detour_CheckBan_IP),
            typeof(BanManager)
                .GetNestedTypes(_bfany)
                .SelectMany(i => i.GetMethods(_bfany).Where(m => m.Name.Contains("CheckBan")))
                .First(),
            this.Detour_CheckBan_IP);
        this.ILHook(
            nameof(this.ILHook_Mitigation_DisabledInvincible),
            Utils.TShockType("Bouncer")
                .GetMethod("OnPlayerDamage", _bfany)!,
            this.ILHook_Mitigation_DisabledInvincible);
        this.ILHook(
            nameof(this.ILHook_Mitigation_KeepRestAlive),
            typeof(Rests.Rest)
                .GetMethod("OnRequest", _bfany)!,
            this.ILHook_Mitigation_KeepRestAlive);
    }

    private void LoadConfig(TSPlayer? initiator)
    {
        var jss = new JsonSerializerSettings
        {
            ObjectCreationHandling = ObjectCreationHandling.Replace,
            Converters = new List<JsonConverter>
            {
                new Config.LimiterConfig.LimiterConverter(),
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
            initiator?.SendErrorMessage($"Failed to load config: {ex.Message}");
            return;
        }
        try
        {
            File.WriteAllText(this.ConfigPath, JsonConvert.SerializeObject(this.config, jss));
        }
        catch (Exception ex)
        {
            initiator?.SendErrorMessage($"Failed to save config: {ex.Message}");
            return;
        }
        initiator?.SendSuccessMessage("Chireiden.Omni loaded.");
    }

    private void ApplyConfig(TSPlayer? initiator)
    {
        if (this.config.ShowConfig)
        {
            initiator?.SendInfoMessage(JsonConvert.SerializeObject(this.config, Formatting.Indented));
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
            initiator?.SendInfoMessage("ChatSpam limit applied:");
            foreach (var limiter in spamlim)
            {
                initiator?.SendInfoMessage($"  {Math.Round(limiter.Maximum / limiter.RateLimit, 1):G} messages per {Math.Round(limiter.Maximum / 60, 1):G} seconds");
            }
        }
        var connlim = this.config.Mitigation.ConnectionLimit;
        if (connlim.Count > 0)
        {
            initiator?.SendInfoMessage("Connection limit applied:");
            foreach (var limiter in connlim)
            {
                initiator?.SendInfoMessage($"  {Math.Round(limiter.Maximum / limiter.RateLimit, 1):G} connections per IP per {Math.Round(limiter.Maximum, 1):G} seconds");
            }
        }
        foreach (var field in typeof(DefinedConsts.DataKey).GetFields())
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
        Terraria.Initializers.ChatInitializer.Load();
    }

    private void OnReload(ReloadEventArgs? e)
    {
        this.LoadConfig(e?.Player);
        this.ApplyConfig(e?.Player);
    }

    public override void Initialize()
    {
        this.config._init = true;
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
        TShockAPI.GetDataHandlers.Sign.Register(this.GDHook_Permission_Sign);
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
            TShockAPI.GetDataHandlers.Sign.UnRegister(this.GDHook_Permission_Sign);
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
        this.Backports();
        OTAPI.Hooks.Netplay.CreateTcpListener += this.OTHook_Socket_OnCreate;
        this.InitCommands();
        this.OnReload(new ReloadEventArgs(TSPlayer.Server));
    }

    private void ShowError(string value)
    {
        if (this.config?._init == true)
        {
            TShockAPI.TShock.Log.Error(value);
        }
        else
        {
            Console.WriteLine(value);
        }
    }
}