using Terraria.Localization;
using TerrariaApi.Server;
using static TShockAPI.GetDataHandlers;

namespace Chireiden.TShock.Omni;

public partial class Plugin : TerrariaPlugin
{
    private void Hook_Permission_TogglePvp(object? sender, TogglePvpEventArgs args)
    {
        if (args.Handled)
        {
            return;
        }

        var restrict = this.config.Permission.Restrict;
        if (!restrict.Enabled || !restrict.TogglePvP)
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
        if (args.Handled)
        {
            return;
        }

        var restrict = this.config.Permission.Restrict;
        if (!restrict.Enabled || !restrict.ToggleTeam)
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

    private void Hook_Permission_SyncLoadout(object? sender, OTAPI.Hooks.MessageBuffer.GetDataEventArgs args)
    {
        if (args.Result == OTAPI.HookResult.Cancel)
        {
            return;
        }

        if (args.PacketId != (int) PacketTypes.SyncLoadout)
        {
            return;
        }

        var restrict = this.config.Permission.Restrict;
        if (!restrict.Enabled || !restrict.SyncLoadout)
        {
            return;
        }

        var player = TShockAPI.TShock.Players[args.Instance.whoAmI];
        if (player == null || player.HasPermission(Consts.Permissions.SyncLoadout))
        {
            return;
        }

        args.Result = OTAPI.HookResult.Cancel;
        // FIXME: TSAPI is not respecting args.Result, so we have to craft invalid packet. Switch to Loadout 255 when only 3.
        args.Instance.readBuffer[args.ReadOffset + 1] = 255;
        Terraria.NetMessage.TrySendData((int) PacketTypes.SyncLoadout, -1, -1, null, args.Instance.whoAmI, player.TPlayer.CurrentLoadoutIndex);
    }
}
