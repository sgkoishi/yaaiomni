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
    public override string Name => $"{Assembly.GetExecutingAssembly().GetName().Name} {new string(CommitHashAttribute.GetCommitHash().Take(10).ToArray())}";
    public override string Author => "SGKoishi";
    public override Version Version => Assembly.GetExecutingAssembly().GetName().Version!;
    public override string Description => Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description!;
    public override string UpdateURL => Assembly.GetExecutingAssembly()
        .GetCustomAttributes<AssemblyMetadataAttribute>()
        .FirstOrDefault(x => x.Key == "RepositoryUrl")?.Value!;

    public string ConfigPath = Path.Combine(TShockAPI.TShock.SavePath, DefinedConsts.Misc.ConfigFile);
    private const BindingFlags _bfany = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    public Config config = null!;

    public Plugin(Main game) : base(game)
    {
        Utils.AssemblyMutex(this);
        AppDomain.CurrentDomain.AssemblyResolve += this.AssemblyResolveHandler;
        AppDomain.CurrentDomain.FirstChanceException += this.FirstChanceExceptionHandler;
        var pa = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture;
        if (pa is not System.Runtime.InteropServices.Architecture.X64 and not System.Runtime.InteropServices.Architecture.X86)
        {
            Console.WriteLine($"TShock is running under {pa}, some features may not work.");
        }

        this.Order = -1_000_000;
        this.ReadConfig(Utils.ConsolePlayer.Instance, true);

        if (this.config.Enhancements.Value.DefaultLanguageDetect)
        {
            this.ResetGameLocale();
        }

        {
            var encoding = this.config.Soundness.Value.UseDefaultEncoding.Value;
            if (encoding != 0)
            {
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                if (encoding == -1)
                {
                    Console.OutputEncoding = System.Text.Encoding.GetEncoding(System.Globalization.CultureInfo.CurrentUICulture.TextInfo.ANSICodePage);
                    Console.WriteLine($"Console encoding set to default ({Console.OutputEncoding.EncodingName})");
                }
                else
                {
                    Console.OutputEncoding = System.Text.Encoding.GetEncoding(encoding);
                    Console.WriteLine($"Console encoding set to {Console.OutputEncoding.EncodingName}");
                }
            }
        }

        this.Detour(
            nameof(this.Detour_UpdateCheckAsync),
            typeof(UpdateManager)
                .GetMethod(nameof(UpdateManager.UpdateCheckAsync), _bfany),
            this.Detour_UpdateCheckAsync);
        this.Detour(
            nameof(this.Detour_HasPermission),
            typeof(TSPlayer)
                .GetMethod(nameof(TSPlayer.HasPermission), _bfany, [typeof(string)]),
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
        this.Detour(
            nameof(this.Detour_HelpAliases),
            typeof(Commands)
                .GetNestedTypes(_bfany)
                .SelectMany(i => i.GetMethods(_bfany).Where(m => m.DeclaringType!.Name == "<>c" && m.Name.StartsWith("<Help>")))
                .FirstOrDefault(),
            this.Detour_HelpAliases);
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
        this.Detour(
          nameof(this.Detour_Mitigation_HandleSyncLoadout),
           typeof(TShockAPI.GetDataHandlers)
                .GetMethod("HandleSyncLoadout", _bfany),
          this.Detour_Mitigation_HandleSyncLoadout
        );
        this.Detour(
            nameof(this.Detour_Socket_StartDualMode),
            typeof(System.Net.Sockets.TcpListener)
                .GetMethod(nameof(System.Net.Sockets.TcpListener.Start), [typeof(int)]),
            this.Detour_Socket_StartDualMode
        );
        this.Detour(
            nameof(this.Detour_RealIP_IPv6Support),
            typeof(TShockAPI.Utils)
                .GetMethod(nameof(TShockAPI.Utils.GetRealIP), _bfany),
            this.Detour_RealIP_IPv6Support
        );
        this.Detour(
            nameof(this.Detour_Main_getWorldPathName),
            typeof(Terraria.Main)
                .GetProperty(nameof(Terraria.Main.worldPathName), _bfany)?
                .GetMethod,
            this.Detour_Main_getWorldPathName
        );

        if (this.config.PrioritizedPacketHandle)
        {
            Utils.RegisterFirst<EventHandler<OTAPI.Hooks.MessageBuffer.GetDataEventArgs>>(typeof(OTAPI.Hooks.MessageBuffer),
                nameof(OTAPI.Hooks.MessageBuffer.GetData),
                null,
                this.OTHook_Modded_GetData,
                this.OTHook_Mitigation_GetData);
        }
    }

    private Assembly? AssemblyResolveHandler(object? sender, ResolveEventArgs args)
    {
        var an = new AssemblyName(args.Name);
        if (an.Name == Assembly.GetExecutingAssembly().GetName().Name)
        {
            return Assembly.GetExecutingAssembly();
        }
        if (this.config.Enhancements.Value.ResolveAssembly)
        {
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (a.GetName().Name == an.Name && a.GetName().Version >= an.Version)
                {
                    return a;
                }
            }
        }
        return null;
    }

    public event Action<Plugin, Config>? OnConfigLoad;

    private void ReadConfig(TSPlayer? initiator, bool silent = true)
    {
        var prev = this.config;
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
        }

        this.config ??= new Config();
        OnConfigLoad?.Invoke(this, prev);

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
    }

    private void ApplyConfig(TSPlayer? initiator)
    {
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
        Terraria.Initializers.ChatInitializer.Load();
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
        foreach (var command in this.config.StartupCommands.Value)
        {
            TShockAPI.Commands.HandleCommand(TShockAPI.TSPlayer.Server, command);
        }
        foreach (var p in TerrariaApi.Server.ServerApi.Plugins)
        {
            if (p.Plugin is TerrariaPlugin plugin)
            {
                if (plugin.Name.Contains("Dimensions") || ((Action) plugin.Initialize).Method.DeclaringType?.FullName?.Contains("Dimensions") == true)
                {
                    // Dimensions use Placeholder 67 to show the IP address
                    this.AllowedPackets.Add(PacketTypes.Placeholder);
                }
            }
        }
        if (this.config.Enhancements.Value.BanPattern)
        {
            if (!TShockAPI.DB.Identifier.Available.Any(i => i.Prefix == "namea:"))
            {
                TShockAPI.DB.Identifier.Register("namea:", "An identifier for Regex matching the character name (e.g. namea:^.{8,}$)");
            }
            if (!TShockAPI.DB.Identifier.Available.Any(i => i.Prefix == "ipa:"))
            {
                TShockAPI.DB.Identifier.Register("ipa:", "An identifier for IP address with subnet mask (e.g. ipa:1.2.3.4/24)");
            }
        }
    }

    private void OnReload(ReloadEventArgs? e)
    {
        this.ReadConfig(e?.Player, false);
        this.ApplyConfig(e?.Player);
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
        if (this.config.Mitigation.Value.ReloadILHook)
        {
            foreach (var hook in this._manipulators.Values)
            {
                hook.Undo();
                hook.Apply();
            }
        }
        e?.Player?.SendSuccessMessage($"{this.Name} loaded.");
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
        On.Terraria.WorldGen.KillTile += this.MMHook_WorldGen_KillTile;
        On.Terraria.WorldGen.TileFrame += this.MMHook_WorldGen_TileFrame;
        On.Terraria.Chest.ServerPlaceItem += this.MMHook_Chest_ServerPlaceItem;
        On.Terraria.RemoteClient.Reset += this.MMHook_RemoteClient_Reset;
        OTAPI.Hooks.NetMessage.SendBytes += this.OTHook_Ghost_SendBytes;
        OTAPI.Hooks.NetMessage.SendBytes += this.OTHook_DebugPacket_SendBytes;
        OTAPI.Hooks.MessageBuffer.GetData += this.OTHook_Ping_GetData;
        OTAPI.Hooks.MessageBuffer.GetData += this.OTHook_TileEntity_Interaction;
        if (!this.config.PrioritizedPacketHandle)
        {
            OTAPI.Hooks.MessageBuffer.GetData += this.OTHook_Modded_GetData;
            OTAPI.Hooks.MessageBuffer.GetData += this.OTHook_Mitigation_GetData;
        }
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
        TShockAPI.GetDataHandlers.PlayerBuffUpdate.Register(this.GDHook_Mitigation_PlayerBuffUpdate);
        Terraria.Localization.LanguageManager.Instance.OnLanguageChanged += this.RedirectLanguage;
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
            On.Terraria.WorldGen.KillTile -= this.MMHook_WorldGen_KillTile;
            On.Terraria.WorldGen.TileFrame -= this.MMHook_WorldGen_TileFrame;
            On.Terraria.Chest.ServerPlaceItem -= this.MMHook_Chest_ServerPlaceItem;
            On.Terraria.RemoteClient.Reset -= this.MMHook_RemoteClient_Reset;
            OTAPI.Hooks.NetMessage.SendBytes -= this.OTHook_Ghost_SendBytes;
            OTAPI.Hooks.NetMessage.SendBytes -= this.OTHook_DebugPacket_SendBytes;
            OTAPI.Hooks.MessageBuffer.GetData -= this.OTHook_Mitigation_GetData;
            OTAPI.Hooks.MessageBuffer.GetData -= this.OTHook_Modded_GetData;
            OTAPI.Hooks.MessageBuffer.GetData -= this.OTHook_Ping_GetData;
            OTAPI.Hooks.MessageBuffer.GetData -= this.OTHook_TileEntity_Interaction;
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
            TShockAPI.GetDataHandlers.Sign.UnRegister(this.GDHook_Permission_Sign);
            TShockAPI.GetDataHandlers.NPCAddBuff.UnRegister(this.GDHook_Mitigation_NpcAddBuff);
            TShockAPI.GetDataHandlers.PlayerBuffUpdate.UnRegister(this.GDHook_Mitigation_PlayerBuffUpdate);
            Terraria.Localization.LanguageManager.Instance.OnLanguageChanged -= this.RedirectLanguage;
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
        On.Terraria.NetMessage.SendData += this.MMHook_DebugPacket_SendData;
        On.Terraria.MessageBuffer.GetData += this.MMHook_DebugPacket_GetData;
        On.Terraria.NetMessage.SendData += this.MMHook_DebugPacket_CatchSend;
        On.Terraria.MessageBuffer.GetData += this.MMHook_DebugPacket_CatchGet;
        this.RefreshLocalizedCommandAliases();
        this.ApplyConfig(TSPlayer.Server);
    }

    private void PostTShockInitialize()
    {
        this.Backports();
        OTAPI.Hooks.Netplay.CreateTcpListener += this.OTHook_Socket_OnCreate;
        this.InitCommands();
        if (this.config.Enhancements.Value.IPv6DualStack)
        {
            if (!Terraria.Program.LaunchParameters.TryAdd("-ip", System.Net.IPAddress.IPv6Any.ToString()))
            {
                TShockAPI.TShock.Log.Warn("Listening on existing address, attempts to enable IPv6 dual-stack without -ip.");
            }
        }
    }
}