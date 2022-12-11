using MonoMod.RuntimeDetour;
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

    public static string ConfigPath = Path.Combine(TShockAPI.TShock.SavePath, "chireiden.omni.json");

    public Config config;

    public Plugin(Main game) : base(game)
    {
        this.Order = int.MinValue;
        this.config = new Config();
        this._UpdateCheckAsyncDetour = new Hook(
            typeof(UpdateManager)
                .GetMethod(nameof(UpdateManager.UpdateCheckAsync), BindingFlags.Public | BindingFlags.Instance)!,
            this.UpdateCheckAsync);
        this._TSPlayerHasPermissionDetour = new Hook(
            typeof(TSPlayer)
                .GetMethod(nameof(TSPlayer.HasPermission), BindingFlags.Public | BindingFlags.Instance)!,
            this.HasPermission);
    }

    private void OnReload(ReloadEventArgs? e)
    {
        try
        {
            if (!File.Exists(ConfigPath))
            {
                File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(this.config, Formatting.Indented));
            }
            else
            {
                this.config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(ConfigPath))!;
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
        ServerApi.Hooks.NetNameCollision.Register(this, this.NameCollision);
        ServerApi.Hooks.GamePostInitialize.Register(this, this.OnGamePostInitialize);
        GeneralHooks.ReloadEvent += this.OnReload;
    }

    private void OnGamePostInitialize(EventArgs args)
    {
        Commands.ChatCommands.Add(new Command("chireiden.omni.whynot", this.QueryPermissionCheck, "whynot"));
        this.OnReload(new ReloadEventArgs(TSPlayer.Server));
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
            On.Terraria.Projectile.Kill -= this.Soundness_ProjectileKill;
            ServerApi.Hooks.NetNameCollision.Deregister(this, this.NameCollision);
            ServerApi.Hooks.GamePostInitialize.Deregister(this, this.OnGamePostInitialize);
            GeneralHooks.ReloadEvent -= this.OnReload;
            this._UpdateCheckAsyncDetour.Undo();
        }
        base.Dispose(disposing);
    }
}
