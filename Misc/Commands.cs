using Terraria.Localization;
using TerrariaApi.Server;
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

    [Command("Chat", "_chat", Permission = "chireiden.omni.chat")]
    private void Command_Chat(CommandArgs args)
    {
        var index = args.Player.Index;
        var scea = new ServerChatEventArgs();
        var command = Terraria.Chat.ChatCommandId.FromType<Terraria.Chat.Commands.SayChatCommand>();
        typeof(ServerChatEventArgs).GetProperty(nameof(ServerChatEventArgs.Buffer))!.SetValue(scea, Terraria.NetMessage.buffer[index]);
        typeof(ServerChatEventArgs).GetProperty(nameof(ServerChatEventArgs.Who))!.SetValue(scea, index);
        typeof(ServerChatEventArgs).GetProperty(nameof(ServerChatEventArgs.Text))!.SetValue(scea, string.Join(" ", args.Parameters));
        typeof(ServerChatEventArgs).GetProperty(nameof(ServerChatEventArgs.CommandId))!.SetValue(scea, command);
        TerrariaApi.Server.ServerApi.Hooks.ServerChat.Invoke(scea);
    }

    [Command("Echo", "echo", AllowServer = false, Permission = "chireiden.omni.echo")]
    private void Command_Echo(CommandArgs args)
    {
        args.Player.SendInfoMessage(string.Join(" ", args.Parameters));
    }

    [Command("Admin.GenerateFullConfig", "genconfig", Permission = "chireiden.omni.admin.genconfig")]
    private void Command_GenerateFullConfig(CommandArgs args)
    {
        try
        {
            File.WriteAllText(this.ConfigPath, Json.JsonUtils.SerializeConfig(this.config, false));
        }
        catch (Exception ex)
        {
            args.Player.SendErrorMessage($"Failed to save config: {ex.Message}");
        }
    }
}