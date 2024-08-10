using TShockAPI;

namespace Chireiden.TShock.Omni;

partial class Plugin
{
    private void MMHook_TileProvider_ClearWorld(On.Terraria.WorldGen.orig_clearWorld orig)
    {
        var rs = this.config.Enhancements.Value.ExtraLargeWorld.Value
            && (Terraria.Main.maxTilesX < Terraria.Main.tile.Width || Terraria.Main.maxTilesY < Terraria.Main.tile.Height);
        switch (Terraria.Main.tile)
        {
            case ModFramework.DefaultCollection<Terraria.ITile>:
                Terraria.Main.tile = rs ? Terraria.Main.tile : new ModFramework.DefaultCollection<Terraria.ITile>(Terraria.Main.maxTilesX, Terraria.Main.maxTilesY);
                break;
            case TerrariaApi.Server.ConstileationProvider:
                Terraria.Main.tile = rs ? Terraria.Main.tile : new TerrariaApi.Server.ConstileationProvider();
                break;
            case TerrariaApi.Server.TileProvider:
                Terraria.Main.tile = rs ? Terraria.Main.tile : new TerrariaApi.Server.TileProvider();
                break;
            case CheckedTypedCollection ctc:
                ctc.Resize(Terraria.Main.maxTilesX + 1, Terraria.Main.maxTilesY + 1);
                break;
            case CheckedGenericCollection cgc:
                cgc.Resize(Terraria.Main.maxTilesX + 1, Terraria.Main.maxTilesY + 1);
                break;
            default:
            {
                if (Terraria.Main.tile != null && rs)
                {
                    try
                    {
                        Terraria.Main.tile = (ModFramework.ICollection<Terraria.ITile>) Activator.CreateInstance(Terraria.Main.tile.GetType());
                    }
                    catch
                    {
                        TShockAPI.TShock.Log.ConsoleWarn($"Attempt to extend the tile provider because we need {Terraria.Main.maxTilesX}x{Terraria.Main.maxTilesY} but we only have {Terraria.Main.tile.Width}x{Terraria.Main.tile.Height}.");
                        TShockAPI.TShock.Log.ConsoleWarn("Failed to extend the tile provider. The server will probably crash due to lack of array space.");
                        // failed, keep original
                    }
                }
                break;
            }
        }
        GC.Collect();
        orig();
    }

    [Command("Admin.TileProvider", "tileprovider", Permission = "chireiden.omni.admin.tileprovider")]
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
                    new TerrariaApi.Server.TileProvider());
                args.Player.SendSuccessMessage("Tile provider set to heaptile.");
                break;
            case "constilation":
                Terraria.Main.tile = Utils.CloneTileCollection(Terraria.Main.tile,
                    new TerrariaApi.Server.ConstileationProvider());
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