using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Terraria.Localization;
using TShockAPI;

namespace Chireiden.TShock.Omni.Misc;

public partial class Plugin
{
    [Command("PvPStatus", "_pvp", Permission = "chireiden.omni.setpvp")]
    [RelatedPermission("Admin.PvPStatus", "chireiden.omni.admin.setpvp")]
    private void Command_PvP(CommandArgs args)
    {
        if (args.Parameters.Count == 0)
        {
            args.Player.SendInfoMessage($"Your PvP status: {args.Player.TPlayer.hostile}");
            return;
        }

        if (args.Parameters.Count > 1)
        {
            if (!args.Player.HasPermission(DefinedConsts.PermissionsList.Admin.PvPStatus))
            {
                args.Player.SendErrorMessage("You don't have permission to set other players' PvP status.");
                return;
            }

            var player = TSPlayer.FindByNameOrID(args.Parameters[0]);
            if (player.Count == 0)
            {
                args.Player.SendErrorMessage("Invalid player.");
                return;
            }
            else if (player.Count > 1)
            {
                args.Player.SendMultipleMatchError(player.Select(p => p.Name));
                return;
            }

            if (!bool.TryParse(args.Parameters[1], out var pvp))
            {
                args.Player.SendErrorMessage("Invalid PvP status.");
                return;
            }

            player[0].TPlayer.hostile = pvp;
            Terraria.NetMessage.TrySendData((int) PacketTypes.TogglePvp, -1, -1, NetworkText.Empty, player[0].Index);
        }
        else
        {
            if (!bool.TryParse(args.Parameters[0], out var pvp))
            {
                args.Player.SendErrorMessage("Invalid PvP status.");
                return;
            }

            args.Player.TPlayer.hostile = pvp;
            Terraria.NetMessage.TrySendData((int) PacketTypes.TogglePvp, -1, -1, NetworkText.Empty, args.Player.Index);
        }
    }

    [Command("TeamStatus", "_team", Permission = "chireiden.omni.setteam")]
    [RelatedPermission("Admin.TeamStatus", "chireiden.omni.admin.setteam")]
    private void Command_Team(CommandArgs args)
    {
        if (args.Parameters.Count == 0)
        {
            var team = args.Player.TPlayer.team;
            if (team > Terraria.Main.teamColor.Length)
            {
                args.Player.SendErrorMessage("Invalid team.");
                return;
            }

            var color = Terraria.Main.teamColor[team];
            var cc = $"{color.R:X2}{color.G:X2}{color.B:X2}";
            args.Player.SendInfoMessage($"Your team: {team.Color(cc)}");
            return;
        }

        if (args.Parameters.Count > 1)
        {
            if (!args.Player.HasPermission(DefinedConsts.PermissionsList.Admin.TeamStatus))
            {
                args.Player.SendErrorMessage("You don't have permission to set other players' team.");
                return;
            }

            var player = TSPlayer.FindByNameOrID(args.Parameters[0]);
            if (player.Count == 0)
            {
                args.Player.SendErrorMessage("Invalid player.");
                return;
            }
            else if (player.Count > 1)
            {
                args.Player.SendMultipleMatchError(player.Select(p => p.Name));
                return;
            }

            if (!byte.TryParse(args.Parameters[1], out var team))
            {
                args.Player.SendErrorMessage("Invalid team.");
                return;
            }

            player[0].TPlayer.team = team;
            Terraria.NetMessage.TrySendData((int) PacketTypes.PlayerTeam, -1, -1, NetworkText.Empty, player[0].Index);
        }
        else
        {
            if (!byte.TryParse(args.Parameters[0], out var team))
            {
                args.Player.SendErrorMessage("Invalid team.");
                return;
            }

            args.Player.TPlayer.team = team;
            Terraria.NetMessage.TrySendData((int) PacketTypes.PlayerTeam, -1, -1, NetworkText.Empty, args.Player.Index);
        }
    }
}