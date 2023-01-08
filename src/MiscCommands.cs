using System.Reflection;
using Terraria.Localization;
using TerrariaApi.Server;
using TShockAPI;

namespace Chireiden.TShock.Omni;

public partial class Plugin : TerrariaPlugin
{
    private void Command_PvP(CommandArgs args)
    {
        if (args.Parameters.Count == 0)
        {
            args.Player.SendInfoMessage($"Your PvP status: {args.Player.TPlayer.hostile}");
            return;
        }

        if (args.Parameters.Count > 1)
        {
            if (!args.Player.HasPermission(Consts.Permissions.Admin.SetPvp))
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
            Terraria.NetMessage.TrySendData((int)PacketTypes.TogglePvp, -1, -1, NetworkText.Empty, player[0].Index);
        }
        else
        {
            if (!bool.TryParse(args.Parameters[0], out var pvp))
            {
                args.Player.SendErrorMessage("Invalid PvP status.");
                return;
            }

            args.Player.TPlayer.hostile = pvp;
            Terraria.NetMessage.TrySendData((int)PacketTypes.TogglePvp, -1, -1, NetworkText.Empty, args.Player.Index);
        }
    }

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
            if (!args.Player.HasPermission(Consts.Permissions.Admin.SetTeam))
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
            Terraria.NetMessage.TrySendData((int)PacketTypes.PlayerTeam, -1, -1, NetworkText.Empty, player[0].Index);
        }
        else
        {
            if (!byte.TryParse(args.Parameters[0], out var team))
            {
                args.Player.SendErrorMessage("Invalid team.");
                return;
            }

            args.Player.TPlayer.team = team;
            Terraria.NetMessage.TrySendData((int)PacketTypes.PlayerTeam, -1, -1, NetworkText.Empty, args.Player.Index);
        }
    }
    private void Command_GC(CommandArgs args)
    {
        if (args.Parameters.Contains("-f"))
        {
            GC.Collect();
        }
        else
        {
            GC.Collect(3, GCCollectionMode.Optimized, false);
        }
        args.Player.SendSuccessMessage("GC Triggered.");
    }

    private void Command_MaxPlayers(CommandArgs args)
    {
        if (args.Parameters.Count == 0)
        {
            args.Player.SendInfoMessage($"Max players: {Terraria.Main.maxNetPlayers}");
            return;
        }

        if (byte.TryParse(args.Parameters[0], out var maxPlayers))
        {
            Terraria.Main.maxNetPlayers = maxPlayers;
            args.Player.SendSuccessMessage($"Max players set to {maxPlayers}.");
        }
        else
        {
            args.Player.SendErrorMessage("Invalid max players.");
        }
    }

    private void Command_RawBroadcast(CommandArgs args)
    {
        if (args.Parameters.Count == 0)
        {
            args.Player.SendErrorMessage("Invalid broadcast message.");
            return;
        }

        // No need to log because TShock log every command already.
        TSPlayer.All.SendMessage(args.Parameters[0], 0, 0, 0);
    }

    private void Command_Sudo(CommandArgs args)
    {
        if (args.Parameters.Count == 0)
        {
            args.Player.SendErrorMessage("Invalid player.");
            return;
        }

        if (args.Parameters.Count == 1)
        {
            args.Player.SendErrorMessage("Invalid command.");
            return;
        }

        var player = TSPlayer.FindByNameOrID(args.Parameters[0]);
        if (player.Count == 1)
        {
            // Right one
        }
        else if (args.Parameters[0] == "*" || this.config.PlayerWildcardFormat.Contains(args.Parameters[0]))
        {
            player = TShockAPI.TShock.Players.Where(p => p != null && p.Active).ToList();
        }
        else
        {
            if (player.Count == 0)
            {
                args.Player.SendErrorMessage("Invalid player.");
                return;
            }
            if (player.Count > 1)
            {
                args.Player.SendMultipleMatchError(player.Select(p => p.Name));
                return;
            }
        }

        var withoutcheck = args.Parameters.Count > 2 && args.Parameters[2] == "-f";

        if (withoutcheck)
        {
            var cmdargs = (List<string>)typeof(TShockAPI.Command)
                .GetMethod("ParseParameters", BindingFlags.NonPublic | BindingFlags.Static)!
                .Invoke(null, new object[] { args.Parameters[1] })!;
            if (cmdargs.Count == 0)
            {
                args.Player.SendErrorMessage("Invalid command.");
                return;
            }

            var cmdname = cmdargs[0];
            if (!cmdname.StartsWith(Commands.Specifier) && !cmdname.StartsWith(Commands.SilentSpecifier))
            {
                args.Player.SendErrorMessage("Invalid command.");
                return;
            }

            var silent = cmdname.StartsWith(Commands.SilentSpecifier);
            var specifier = silent ? Commands.SilentSpecifier : Commands.Specifier;
            cmdname = cmdname.Substring(specifier.Length);
            var cmds = Commands.ChatCommands.Where(c => c.HasAlias(cmdname)).ToList();
            var cmdtext = args.Parameters[1].Substring(specifier.Length);
            foreach (var p in player)
            {
                var cmds_clone = cmds.ToList().AsEnumerable();
                if (TShockAPI.Hooks.PlayerHooks.OnPlayerCommand(p, cmdname, cmdtext, cmdargs, ref cmds_clone, specifier))
                {
                    continue;
                }

                foreach (var cmd in cmds_clone)
                {
                    if (cmd.DoLog)
                    {
                        TShockAPI.TShock.Log.ConsoleInfo($"{args.Player.Name} force {p.Name} executed: {specifier}{cmdtext}");
                    }
                    else
                    {
                        TShockAPI.TShock.Log.ConsoleInfo($"{args.Player.Name} force {p.Name} executed (args omitted): {specifier}{cmdname}");
                    }

                    cmd.CommandDelegate(new CommandArgs(cmdtext, silent, p, cmdargs));
                }
            }
        }
        else
        {
            foreach (var p in player)
            {
                TShockAPI.Commands.HandleCommand(p, args.Parameters[1]);
            }
        }
    }
}
