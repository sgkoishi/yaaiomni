using TerrariaApi.Server;

namespace Chireiden.TShock.Omni;

public partial class Plugin : TerrariaPlugin
{
    private void TAHook_Permission_ItemForceIntoChest(ForceItemIntoChestEventArgs args)
    {
        if (args.Handled)
        {
            return;
        }

        if (!this.config.Soundness.QuickStackRestriction)
        {
            return;
        }

        var tp = TShockAPI.TShock.Players[args.Player.whoAmI];
        if (tp == null || !tp.HasBuildPermission(args.Chest.x, args.Chest.y, false))
        {
            args.Handled = true;
        }
    }

    private void GDHook_Permission_Sign(object? sender, TShockAPI.GetDataHandlers.SignEventArgs args)
    {
        if (args.Handled)
        {
            return;
        }

        if (!this.config.Soundness.SignEditRestriction)
        {
            return;
        }

        var tp = TShockAPI.TShock.Players[args.Player.Index];
        if (tp == null || !tp.HasBuildPermission(args.X, args.Y, false))
        {
            args.Handled = true;
        }
    }
}