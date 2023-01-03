using Terraria.Localization;
using TerrariaApi.Server;
using static TShockAPI.GetDataHandlers;

namespace Chireiden.TShock.Omni;

public partial class Plugin : TerrariaPlugin
{
    private void Hook_Permission_TogglePvp(object? sender, TogglePvpEventArgs args)
    {
        var restrict = this.config.Permission.Restrict;
        if (!restrict.Enabled)
        {
            return;
        }

        if (!restrict.TogglePvP)
        {
            return;
        }

        if (args.Player.HasPermission(Consts.Permissions.TogglePvP))
        {
            return;
        }

        if (args.Player.HasPermission($"{Consts.Permissions.TogglePvP}.{args.Pvp}"))
        {
            return;
        }

        args.Handled = true;
        Terraria.NetMessage.TrySendData((int) PacketTypes.TogglePvp, -1, -1, NetworkText.Empty, args.PlayerId);
    }

    private void Hook_Permission_PlayerTeam(object? sender, PlayerTeamEventArgs args)
    {
        var restrict = this.config.Permission.Restrict;
        if (!restrict.Enabled)
        {
            return;
        }

        if (!restrict.ToggleTeam)
        {
            return;
        }

        if (args.Player.HasPermission(Consts.Permissions.ToggleTeam))
        {
            return;
        }

        if (args.Player.HasPermission($"{Consts.Permissions.ToggleTeam}.{args.Team}"))
        {
            return;
        }

        args.Handled = true;
        Terraria.NetMessage.TrySendData((int) PacketTypes.PlayerTeam, -1, -1, NetworkText.Empty, args.PlayerId);
    }
}
