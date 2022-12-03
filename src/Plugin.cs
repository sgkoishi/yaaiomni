using MonoMod.RuntimeDetour;
using Newtonsoft.Json;
using System.Reflection;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

[ApiVersion(2, 1)]
public partial class Plugin : TerrariaPlugin
{
    public override string Name => Assembly.GetExecutingAssembly().GetName().Name!;
    public override string Author => "SGKoishi";

    public Config config;

    public Plugin(Main game) : base(game)
    {
        this.config = new Config();
        this._UpdateCheckAsyncDetour = new Hook(
            typeof(UpdateManager)
                .GetMethod("UpdateCheckAsync", BindingFlags.Public | BindingFlags.Instance)!,
            this.UpdateCheckAsync);
    }

    public override void Initialize()
    {
        On.Terraria.MessageBuffer.GetData += this.PatchVersion;
        On.Terraria.GameContent.Tile_Entities.TEDisplayDoll.ctor += this.TEDisplayDoll_ctor;
        On.Terraria.GameContent.Tile_Entities.TEHatRack.ctor += this.TEHatRack_ctor;
        GeneralHooks.ReloadEvent += this.OnReload;
        this.OnReload(new ReloadEventArgs(TSPlayer.Server));
    }

    private void OnReload(ReloadEventArgs? e)
    {
        try
        {
            const string path = "chireiden.omni.json";
            if (!File.Exists(path))
            {
                File.WriteAllText(path, JsonConvert.SerializeObject(this.config));
            }
            else
            {
                this.config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(path))!;
            }
        }
        catch (Exception ex)
        {
            e?.Player?.SendErrorMessage(ex.Message);
            return;
        }
        e?.Player?.SendSuccessMessage("Chireiden.Omni loaded.");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            On.Terraria.MessageBuffer.GetData -= this.PatchVersion;
            On.Terraria.GameContent.Tile_Entities.TEDisplayDoll.ctor -= this.TEDisplayDoll_ctor;
            GeneralHooks.ReloadEvent -= this.OnReload;
            this._UpdateCheckAsyncDetour.Undo();
        }
        base.Dispose(disposing);
    }
}
