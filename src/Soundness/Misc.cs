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
}
