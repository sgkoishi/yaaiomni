using TerrariaApi.Server;

namespace Chireiden.TShock.Omni;

public partial class Plugin : TerrariaPlugin
{
    private void TileProvider_ClearWorld(On.Terraria.WorldGen.orig_clearWorld orig)
    {
        if (Terraria.Main.tile is ModFramework.DefaultCollection<Terraria.ITile> dc)
        {
            dc.Width = Terraria.Main.maxTilesX;
            dc.Height = Terraria.Main.maxTilesY;
        }
        else if (Terraria.Main.tile is CheckedTypedCollection ctc)
        {
            ctc.Resize(Terraria.Main.maxTilesX, Terraria.Main.maxTilesY);
        }
        else if (Terraria.Main.tile is CheckedGenericCollection cgc)
        {
            cgc.Resize(Terraria.Main.maxTilesX, Terraria.Main.maxTilesY);
        }
        orig();
    }
}
