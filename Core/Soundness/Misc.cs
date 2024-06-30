using TerrariaApi.Server;

namespace Chireiden.TShock.Omni;

public partial class Plugin
{
    [RelatedPermission("ItemForceIntoChestWithoutBuildPermission", "chireiden.omni.ific")]
    private void TAHook_Permission_ItemForceIntoChest(ForceItemIntoChestEventArgs args)
    {
        if (args.Handled)
        {
            return;
        }

        if (!this.config.Soundness.Value.QuickStackRestriction)
        {
            return;
        }

        var tp = TShockAPI.TShock.Players[args.Player.whoAmI];
        if (tp?.HasPermission(DefinedConsts.Permission.ItemForceIntoChestWithoutBuildPermission) != true
            && tp?.HasBuildPermission(args.Chest.x, args.Chest.y, false) != true)
        {
            args.Handled = true;
        }
    }

    [RelatedPermission("SignEditWithoutBuildPermission", "chireiden.omni.signedit")]
    private void GDHook_Permission_Sign(object? sender, TShockAPI.GetDataHandlers.SignEventArgs args)
    {
        if (args.Handled)
        {
            return;
        }

        if (!this.config.Soundness.Value.SignEditRestriction)
        {
            return;
        }

        var tp = TShockAPI.TShock.Players[args.Player.Index];
        if (tp?.HasPermission(DefinedConsts.Permission.SignEditWithoutBuildPermission) != true
            && tp?.HasBuildPermission(args.X, args.Y, false) != true)
        {
            args.Handled = true;
        }
    }

    [RelatedPermission("TileEntityInteractionWithoutBuildPermission", "chireiden.omni.objectinteract")]
    private void OTHook_TileEntity_Interaction(object? sender, OTAPI.Hooks.MessageBuffer.GetDataEventArgs args)
    {
        if (args.Result == OTAPI.HookResult.Cancel)
        {
            return;
        }

        if (!this.config.Soundness.Value.ObjectInteractionRestriction)
        {
            return;
        }

        (int X, int Y)? p = (PacketTypes) args.MessageType switch
        {
            PacketTypes.WeaponsRackTryPlacing => (args.Read<short>(0), args.Read<short>(2)),
            PacketTypes.TileEntityHatRackItemSync => Terraria.DataStructures.TileEntity.ByID.TryGetValue(args.Read<int>(1), out var te)
                ? (te is Terraria.GameContent.Tile_Entities.TEHatRack tehr
                    ? (tehr.Position.X, tehr.Position.Y)
                    : null)
                : null,
            PacketTypes.FoodPlatterTryPlacing => (args.Read<short>(0), args.Read<short>(2)),
            _ => null,
        };

        if (p == null)
        {
            return;
        }

        var tp = TShockAPI.TShock.Players[args.Instance.whoAmI];
        if (tp?.HasPermission(DefinedConsts.Permission.TileEntityInteractionWithoutBuildPermission) != true
            && tp?.HasBuildPermission(p.Value.X, p.Value.Y, false) != true)
        {
            args.Result = OTAPI.HookResult.Cancel;
        }
    }
}