using Newtonsoft.Json;
using System.Reflection;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

[ApiVersion(2, 1)]
public class Plugin : TerrariaPlugin
{
    public override string Name => Assembly.GetExecutingAssembly().GetName().Name!;
    public override string Author => "SGKoishi";
    public Config config;
    public Plugin(Main game) : base(game)
    {
        this.config = new Config();
    }

    private static readonly byte[] _versionPacket = new byte[] { 1, 11, 84, 101, 114, 114, 97, 114, 105, 97 };
    private static readonly byte[] _versionCode = Main.curRelease.ToString().Select(Convert.ToByte).ToArray();

    public override void Initialize()
    {
        On.Terraria.MessageBuffer.GetData += this.PatchVersion;
        On.Terraria.GameContent.Tile_Entities.TEDisplayDoll.ctor += this.TEDisplayDoll_ctor;
        On.Terraria.GameContent.Tile_Entities.TEHatRack.ctor += this.TEHatRack_ctor;
        GeneralHooks.ReloadEvent += this.OnReload;
        this.OnReload(new ReloadEventArgs(TSPlayer.Server));
    }

    private void TEDisplayDoll_ctor(On.Terraria.GameContent.Tile_Entities.TEDisplayDoll.orig_ctor orig, Terraria.GameContent.Tile_Entities.TEDisplayDoll self)
    {
        orig(self);
        if (this.config.TrimMemory)
        {
            self._dollPlayer = null;
        }
    }

    private void TEHatRack_ctor(On.Terraria.GameContent.Tile_Entities.TEHatRack.orig_ctor orig, Terraria.GameContent.Tile_Entities.TEHatRack self)
    {
        orig(self);
        if (this.config.TrimMemory)
        {
            self._dollPlayer = null;
        }
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
        }
        base.Dispose(disposing);
    }

    private void PatchVersion(On.Terraria.MessageBuffer.orig_GetData orig, MessageBuffer self, int start, int length, out int messageType)
    {
        if (self.readBuffer[start] == 1 && length == 13)
        {
            if (self.readBuffer.AsSpan(start, 11).SequenceEqual(_versionPacket))
            {
                if (this.config.SyncVersion)
                {
                    Buffer.BlockCopy(_versionCode, 0, self.readBuffer, start + 11, 3);
                }
            }
        }
        orig(self, start, length, out messageType);
    }
}
