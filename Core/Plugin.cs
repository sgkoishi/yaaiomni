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
    public override string Name => $"{Assembly.GetExecutingAssembly().GetName().Name} {CommitHashAttribute.GetCommitHash()[0..10]}";
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
        this.Detour(
            nameof(this.Detour_UpdateCheckAsync),
            typeof(UpdateManager)
                .GetMethod(nameof(UpdateManager.UpdateCheckAsync), _bfany),
            this.Detour_UpdateCheckAsync);
        this.Detour(
            nameof(this.Detour_HasPermission),
            typeof(TSPlayer)
                .GetMethod(nameof(TSPlayer.HasPermission), _bfany, new[] { typeof(string) }),
            this.Detour_HasPermission);
        this.Detour(
            nameof(this.Detour_PlayerActive),
            typeof(TSPlayer)
                .GetProperty(nameof(TSPlayer.Active), _bfany)?
                .GetMethod,
            this.Detour_PlayerActive);
        this.Detour(
            nameof(this.Detour_Wildcard_GetPlayers),
            typeof(TSPlayer)
                .GetMethod(nameof(TSPlayer.FindByNameOrID), _bfany),
            this.Detour_Wildcard_GetPlayers);
        this.Detour(
            nameof(this.Detour_Mitigation_SetTitle),
            typeof(TShockAPI.Utils)
                .GetMethod("SetConsoleTitle", _bfany),
            this.Detour_Mitigation_SetTitle);
        this.Detour(
            nameof(this.Detour_Command_Alternative),
            typeof(TShockAPI.Commands)
                .GetMethod(nameof(TShockAPI.Commands.HandleCommand), _bfany),
            this.Detour_Command_Alternative);
        this.Detour(
            nameof(this.Detour_CheckBan_IP),
            typeof(BanManager)
                .GetNestedTypes(_bfany)
                .SelectMany(i => i.GetMethods(_bfany).Where(m => m.Name.Contains("CheckBan")))
                .FirstOrDefault(),
            this.Detour_CheckBan_IP);
        this.ILHook(
            nameof(this.ILHook_Mitigation_DisabledInvincible),
            Utils.TShockType("Bouncer")
                .GetMethod("OnPlayerDamage", _bfany),
            this.ILHook_Mitigation_DisabledInvincible);
        this.ILHook(
            nameof(this.ILHook_Mitigation_KeepRestAlive),
            typeof(Rests.Rest)
                .GetMethod("OnRequest", _bfany),
            this.ILHook_Mitigation_KeepRestAlive);
        this.Detour(
            nameof(this.Detour_Mitigation_ConfigUpdate),
            typeof(TShockAPI.FileTools)
                .GetMethod("AttemptConfigUpgrade", _bfany),
            this.Detour_Mitigation_ConfigUpdate
        );

        if (this.config.Enhancements.Value.DefaultLanguageDetect)
        {
            this.ResetGameLocale();
        }
    }

    public event Action<Plugin>? OnConfigLoad;

    private void LoadConfig(TSPlayer? initiator)
    {
        try
        {
            if (File.Exists(this.ConfigPath))
            {
                this.config = Json.JsonUtils.DeserializeConfig<Config>(File.ReadAllText(this.ConfigPath));
            }
        }
        catch (Exception ex)
        {
            initiator?.SendErrorMessage($"Failed to load config: {ex.Message}");
            return;
        }

        OnConfigLoad?.Invoke(this);

        try
        {
            if (!Directory.Exists(TShockAPI.TShock.SavePath))
            {
                Directory.CreateDirectory(TShockAPI.TShock.SavePath);
            }
            File.WriteAllText(this.ConfigPath, Json.JsonUtils.SerializeConfig(this.config));
        }
        catch (Exception ex)
        {
            initiator?.SendErrorMessage($"Failed to save config: {ex.Message}");
        }
    }

    private void ApplyConfig(TSPlayer? initiator)
    {
        this.LoadConfig(initiator);
        if (this.config.ShowConfig)
        {
            initiator?.SendInfoMessage(Json.JsonUtils.SerializeConfig(this.config));
        }
        switch (this.config.Enhancements.Value.TileProvider.Value)
        {
            case Config.EnhancementsSettings.TileProviderOptions.CheckedGenericCollection:
                Terraria.Main.tile = Utils.CloneTileCollection(Terraria.Main.tile, new CheckedGenericCollection());
                break;
            case Config.EnhancementsSettings.TileProviderOptions.CheckedTypedCollection:
                Terraria.Main.tile = Utils.CloneTileCollection(Terraria.Main.tile, new CheckedTypedCollection());
                break;
            case Config.EnhancementsSettings.TileProviderOptions.AsIs:
                break;
        }
        this.DefaultPermissionSetup();
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
        var spamlim = this.config.Mitigation.Value.ChatSpamRestrict.Value;
        if (spamlim.Count > 0)
        {
            initiator?.SendInfoMessage("ChatSpam limit applied:");
            foreach (var limiter in spamlim)
            {
                initiator?.SendInfoMessage($"  {Math.Round(limiter.Maximum / limiter.RateLimit, 1):G} messages per {Math.Round(limiter.Maximum, 1):G} seconds");
            }
        }
        var connlim = this.config.Mitigation.Value.ConnectionLimit.Value;
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
        foreach (var command in this.config.StartupCommands.Value)
        {
            TShockAPI.Commands.HandleCommand(TShockAPI.TSPlayer.Server, command);
        }
        Terraria.Initializers.ChatInitializer.Load();
        initiator?.SendSuccessMessage($"{this.Name} loaded.");
    }

    private void OnReload(ReloadEventArgs? e)
    {
        this.ApplyConfig(e?.Player);
    }

    public override void Initialize()
    {
        On.Terraria.GameContent.Tile_Entities.TEDisplayDoll.ctor += this.MMHook_MemoryTrim_DisplayDoll;
        On.Terraria.GameContent.Tile_Entities.TEHatRack.ctor += this.MMHook_MemoryTrim_HatRack;
        On.Terraria.Projectile.Kill += this.MMHook_Soundness_ProjectileKill;
        On.Terraria.WorldGen.clearWorld += this.MMHook_TileProvider_ClearWorld;
        On.Terraria.Netplay.OnConnectionAccepted += this.MMHook_Mitigation_OnConnectionAccepted;
        On.Terraria.Main.ReadLineInput += this.MMHook_CliConfig_ReadLine;
        On.Terraria.Localization.Language.GetTextValue_string += this.MMHook_CliConfig_LanguageText;
        On.Terraria.WorldGen.DropDoorItem += this.MMHook_Mitigation_DoorDropItem;
        On.Terraria.WorldGen.KillTile_GetItemDrops += this.MMHook_Mitigation_TileDropItem;
        On.Terraria.Initializers.ChatInitializer.Load += this.MMHook_Mitigation_I18nCommand;
        On.Terraria.WorldGen.nextCount += this.MMHook_Mitigation_WorldGenNextCount;
        OTAPI.Hooks.NetMessage.SendBytes += this.OTHook_Ghost_SendBytes;
        OTAPI.Hooks.MessageBuffer.GetData += this.OTHook_Ping_GetData;
        TerrariaApi.Server.ServerApi.Hooks.NetNameCollision.Register(this, this.TAHook_NameCollision);
        TerrariaApi.Server.ServerApi.Hooks.GamePostInitialize.Register(this, this.OnGamePostInitialize);
        TerrariaApi.Server.ServerApi.Hooks.GameUpdate.Register(this, this.TAHook_Update);
        TerrariaApi.Server.ServerApi.Hooks.GameUpdate.Register(this, this.TAHook_Mitigation_GameUpdate);
        TerrariaApi.Server.ServerApi.Hooks.ItemForceIntoChest.Register(this, this.TAHook_Permission_ItemForceIntoChest);
        TShockAPI.Hooks.PlayerHooks.PlayerCommand += this.TSHook_HideCommand_PlayerCommand;
        TShockAPI.Hooks.PlayerHooks.PlayerCommand += this.TSHook_Wildcard_PlayerCommand;
        TShockAPI.Hooks.PlayerHooks.PlayerPermission += this.TSHook_Sudo_OnPlayerPermission;
        TShockAPI.Hooks.GeneralHooks.ReloadEvent += this.OnReload;
        TShockAPI.TShock.Initialized += this.PostTShockInitialize;
        TShockAPI.GetDataHandlers.Sign.Register(this.GDHook_Permission_Sign);
        TShockAPI.GetDataHandlers.NPCAddBuff.Register(this.GDHook_Mitigation_NpcAddBuff);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
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
            On.Terraria.WorldGen.DropDoorItem -= this.MMHook_Mitigation_DoorDropItem;
            On.Terraria.WorldGen.KillTile_GetItemDrops -= this.MMHook_Mitigation_TileDropItem;
            On.Terraria.Initializers.ChatInitializer.Load -= this.MMHook_Mitigation_I18nCommand;
            On.Terraria.WorldGen.nextCount -= this.MMHook_Mitigation_WorldGenNextCount;
            OTAPI.Hooks.NetMessage.SendBytes -= this.OTHook_Ghost_SendBytes;
            OTAPI.Hooks.NetMessage.SendBytes -= this.OTHook_DebugPacket_SendBytes;
            OTAPI.Hooks.MessageBuffer.GetData -= this.OTHook_Mitigation_GetData;
            OTAPI.Hooks.MessageBuffer.GetData -= this.OTHook_Modded_GetData;
            OTAPI.Hooks.MessageBuffer.GetData -= this.OTHook_Ping_GetData;
            OTAPI.Hooks.Netplay.CreateTcpListener -= this.OTHook_Socket_OnCreate;
            TerrariaApi.Server.ServerApi.Hooks.NetNameCollision.Deregister(this, this.TAHook_NameCollision);
            TerrariaApi.Server.ServerApi.Hooks.GamePostInitialize.Deregister(this, this.OnGamePostInitialize);
            TerrariaApi.Server.ServerApi.Hooks.GameUpdate.Deregister(this, this.TAHook_Update);
            TerrariaApi.Server.ServerApi.Hooks.GameUpdate.Deregister(this, this.TAHook_Mitigation_GameUpdate);
            TShockAPI.Hooks.PlayerHooks.PlayerCommand -= this.TSHook_HideCommand_PlayerCommand;
            TShockAPI.Hooks.PlayerHooks.PlayerCommand -= this.TSHook_Wildcard_PlayerCommand;
            TShockAPI.Hooks.PlayerHooks.PlayerPermission -= this.TSHook_Sudo_OnPlayerPermission;
            TShockAPI.Hooks.GeneralHooks.ReloadEvent -= this.OnReload;
            TShockAPI.TShock.Initialized -= this.PostTShockInitialize;
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
        OTAPI.Hooks.MessageBuffer.GetData += this.OTHook_Modded_GetData;
        OTAPI.Hooks.NetMessage.SendBytes += this.OTHook_DebugPacket_SendBytes;
        On.Terraria.NetMessage.SendData += this.MMHook_DebugPacket_SendData;
        On.Terraria.MessageBuffer.GetData += this.MMHook_DebugPacket_GetData;
        On.Terraria.NetMessage.SendData += this.MMHook_DebugPacket_CatchSend;
        On.Terraria.MessageBuffer.GetData += this.MMHook_DebugPacket_CatchGet;
    }

    private void PostTShockInitialize()
    {
        this.Backports();
        OTAPI.Hooks.Netplay.CreateTcpListener += this.OTHook_Socket_OnCreate;
        this.InitCommands();
        this.OnReload(new ReloadEventArgs(TSPlayer.Server));
    }
}