using TerrariaApi.Server;
using TShockAPI;

namespace Chireiden.TShock.Omni;

public partial class Plugin : TerrariaPlugin
{
    private void MMHook_TileProvider_ClearWorld(On.Terraria.WorldGen.orig_clearWorld orig)
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

    private void Command_TileProvider(CommandArgs args)
    {
        var opt = args.Parameters.Count == 0 ? string.Empty : args.Parameters[0].ToLower();
        switch (opt)
        {
            case "default":
                Terraria.Main.tile = Utils.CloneTileCollection(Terraria.Main.tile,
                    new ModFramework.DefaultCollection<Terraria.ITile>(Terraria.Main.maxTilesX, Terraria.Main.maxTilesY));
                args.Player.SendSuccessMessage("Tile provider set to default.");
                break;
            case "heaptile":
                Terraria.Main.tile = Utils.CloneTileCollection(Terraria.Main.tile,
                    new TileProvider());
                args.Player.SendSuccessMessage("Tile provider set to heaptile.");
                break;
            case "constilation":
                Terraria.Main.tile = Utils.CloneTileCollection(Terraria.Main.tile,
                    new ConstileationProvider());
                args.Player.SendSuccessMessage("Tile provider set to constilation.");
                break;
            case "checkedtyped":
                Terraria.Main.tile = Utils.CloneTileCollection(Terraria.Main.tile,
                    new CheckedTypedCollection());
                args.Player.SendSuccessMessage("Tile provider set to checkedtyped.");
                break;
            case "checkedgeneric":
                Terraria.Main.tile = Utils.CloneTileCollection(Terraria.Main.tile,
                    new CheckedGenericCollection());
                args.Player.SendSuccessMessage("Tile provider set to checkedgeneric.");
                break;
            default:
                args.Player.SendErrorMessage("Usage: /tileprovider <default|heaptile|constilation|checkedtyped|checkedgeneric>");
                args.Player.SendInfoMessage("constilation and heaptile require less memory, but are slower.");
                return;
        }
        GC.Collect();
    }
}