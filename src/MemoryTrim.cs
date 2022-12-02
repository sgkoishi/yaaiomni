using TerrariaApi.Server;

public partial class Plugin : TerrariaPlugin
{
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
}
