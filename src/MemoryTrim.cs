using TerrariaApi.Server;

namespace Chireiden.TShock.Omni;

public partial class Plugin : TerrariaPlugin
{
    private void Hook_MemoryTrim_DisplayDoll(On.Terraria.GameContent.Tile_Entities.TEDisplayDoll.orig_ctor orig, Terraria.GameContent.Tile_Entities.TEDisplayDoll self)
    {
        orig(self);
        if (this.config.TrimMemory)
        {
            self._dollPlayer = null;
        }
    }

    private void Hook_MemoryTrim_HatRack(On.Terraria.GameContent.Tile_Entities.TEHatRack.orig_ctor orig, Terraria.GameContent.Tile_Entities.TEHatRack self)
    {
        orig(self);
        if (this.config.TrimMemory)
        {
            self._dollPlayer = null;
        }
    }
}
