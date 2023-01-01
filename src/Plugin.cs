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
        this.Order = int.MinValue;
        this.config = new Config();
        this.Detour(
            nameof(this.UpdateCheckAsync),
            typeof(UpdateManager)
                .GetMethod(nameof(UpdateManager.UpdateCheckAsync), BindingFlags.Public | BindingFlags.Instance)!,
            this.UpdateCheckAsync);
        this.Detour(
            nameof(this.HasPermission),
            typeof(TSPlayer)
                .GetMethod(nameof(TSPlayer.HasPermission), BindingFlags.Public | BindingFlags.Instance, new[] { typeof(string) })!,
            this.HasPermission);
        this.Detour(
            nameof(this.PlayerActive),
            typeof(TSPlayer)
                .GetProperty(nameof(TSPlayer.Active), BindingFlags.Public | BindingFlags.Instance)!
                .GetMethod!,
            this.PlayerActive);
    }

    private void OnReload(ReloadEventArgs? e)
    {
        try
        {
            if (!File.Exists(this.ConfigPath))
            {
                File.WriteAllText(this.ConfigPath, JsonConvert.SerializeObject(this.config, Formatting.Indented));
            }
            else
            {
                this.config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(this.ConfigPath))!;
            }
        }
        catch (Exception ex)
        {
            e?.Player?.SendErrorMessage(ex.Message);
            return;
        }
        e?.Player?.SendSuccessMessage("Chireiden.Omni loaded.");
        if (this.config.ShowConfig)
        {
            e?.Player?.SendInfoMessage(JsonConvert.SerializeObject(this.config, Formatting.Indented));
        }
        switch (this.config.TileProvider)
        {
            case Config.TileProviderOptions.CheckedGenericCollection:
                Terraria.Main.tile = new CheckedGenericCollection();
                break;
            case Config.TileProviderOptions.CheckedTypedCollection:
                Terraria.Main.tile = new CheckedTypedCollection();
                break;
            case Config.TileProviderOptions.AsIs:
                break;
        }
        this.PermissionSetup();
        this.VanillaSetup();
    }

    public override void Initialize()
    {
        On.Terraria.MessageBuffer.GetData += this.PatchVersion;
        On.Terraria.GameContent.Tile_Entities.TEDisplayDoll.ctor += this.MemoryTrim_DisplayDoll;
        On.Terraria.GameContent.Tile_Entities.TEHatRack.ctor += this.MemoryTrim_HatRack;
        On.Terraria.NetMessage.SendData += this.DebugPacket_SendData;
        On.Terraria.MessageBuffer.GetData += this.DebugPacket_GetData;
        On.Terraria.Projectile.Kill += this.Soundness_ProjectileKill;
        On.Terraria.WorldGen.clearWorld += this.TileProvider_ClearWorld;
        OTAPI.Hooks.NetMessage.SendBytes += this.Ghost_SendBytes;
        OTAPI.Hooks.NetMessage.SendBytes += this.DebugPacket_SendBytes;
        TerrariaApi.Server.ServerApi.Hooks.NetNameCollision.Register(this, this.NameCollision);
        TerrariaApi.Server.ServerApi.Hooks.GamePostInitialize.Register(this, this.OnGamePostInitialize);
        TShockAPI.Hooks.PlayerHooks.PlayerCommand += this.PlayerCommand;
        TShockAPI.Hooks.GeneralHooks.ReloadEvent += this.OnReload;
        TShockAPI.TShock.Initialized += this.PostTShockInitialize;
        TShockAPI.GetDataHandlers.TogglePvp.Register(this.TogglePvp);
        TShockAPI.GetDataHandlers.PlayerTeam.Register(this.PlayerTeam);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            On.Terraria.MessageBuffer.GetData -= this.PatchVersion;
            On.Terraria.GameContent.Tile_Entities.TEDisplayDoll.ctor -= this.MemoryTrim_DisplayDoll;
            On.Terraria.GameContent.Tile_Entities.TEHatRack.ctor -= this.MemoryTrim_HatRack;
            On.Terraria.NetMessage.SendData -= this.DebugPacket_SendData;
            On.Terraria.MessageBuffer.GetData -= this.DebugPacket_GetData;
            On.Terraria.NetMessage.SendData -= this.DebugPacket_CatchSend;
            On.Terraria.MessageBuffer.GetData -= this.DebugPacket_CatchGet;
            On.Terraria.Projectile.Kill -= this.Soundness_ProjectileKill;
            On.Terraria.WorldGen.clearWorld -= this.TileProvider_ClearWorld;
            OTAPI.Hooks.NetMessage.SendBytes -= this.Ghost_SendBytes;
            OTAPI.Hooks.NetMessage.SendBytes -= this.DebugPacket_SendBytes;
            OTAPI.Hooks.MessageBuffer.GetData -= this.Mitigation_GetData;
            OTAPI.Hooks.Netplay.CreateTcpListener -= this.OnCreateSocket;
            TerrariaApi.Server.ServerApi.Hooks.NetNameCollision.Deregister(this, this.NameCollision);
            TerrariaApi.Server.ServerApi.Hooks.GamePostInitialize.Deregister(this, this.OnGamePostInitialize);
            TShockAPI.Hooks.PlayerHooks.PlayerCommand -= this.PlayerCommand;
            TShockAPI.Hooks.GeneralHooks.ReloadEvent -= this.OnReload;
            TShockAPI.TShock.Initialized -= this.PostTShockInitialize;
            TShockAPI.GetDataHandlers.TogglePvp.UnRegister(this.TogglePvp);
            TShockAPI.GetDataHandlers.PlayerTeam.UnRegister(this.PlayerTeam);
            foreach (var detour in this._detours.Values)
            {
                detour.Dispose();
            }
        }
        base.Dispose(disposing);
    }

    private void OnGamePostInitialize(EventArgs args)
    {
        OTAPI.Hooks.MessageBuffer.GetData += this.Mitigation_GetData;
        On.Terraria.NetMessage.SendData += this.DebugPacket_CatchSend;
        On.Terraria.MessageBuffer.GetData += this.DebugPacket_CatchGet;
    }

    private void PostTShockInitialize()
    {
        OTAPI.Hooks.Netplay.CreateTcpListener += this.OnCreateSocket;
        Commands.ChatCommands.Add(new Command(Consts.Permissions.Whynot, this.QueryPermissionCheck, Consts.Commands.Whynot));
        Commands.ChatCommands.Add(new Command(Consts.Permissions.Admin.Ghost, this.GhostCommand, Consts.Commands.Ghost));
        Commands.ChatCommands.Add(new Command(Consts.Permissions.Admin.SetLanguage, this.LangCommand, Consts.Commands.SetLanguage));
        Commands.ChatCommands.Add(new Command(Consts.Permissions.PvPCommand, this.PvPCommand, Consts.Commands.SetPvp));
        Commands.ChatCommands.Add(new Command(Consts.Permissions.TeamCommand, this.TeamCommand, Consts.Commands.SetTeam));
        Commands.ChatCommands.Add(new Command(new List<string> { Consts.Permissions.Admin.TriggerGarbageCollection, Permissions.maintenance },
            this.GCCommand, Consts.Commands.TriggerGarbageCollection));
        Commands.ChatCommands.Add(new Command(Consts.Permissions.Admin.DebugStat, this.DebugStatCommand, Consts.Commands.DebugStat));
        Commands.ChatCommands.Add(new Command(Consts.Permissions.Admin.MaxPlayers, this.MaxPlayersCommand, Consts.Commands.MaxPlayers));
        this.OnReload(new ReloadEventArgs(TSPlayer.Server));
    }
}
