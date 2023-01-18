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
            player = Utils.ActivePlayers.ToList();
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
            var cmdargs = (List<string>) typeof(TShockAPI.Command)
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
            cmdname = cmdname[specifier.Length..];
            var cmds = Commands.ChatCommands.Where(c => c.HasAlias(cmdname)).ToList();
            var cmdtext = args.Parameters[1][specifier.Length..];
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

    private void Command_ListConnected(CommandArgs args)
    {
        foreach (var client in Terraria.Netplay.Clients)
        {
            if (client.IsConnected())
            {
                args.Player.SendInfoMessage($"Index: {client.Id} {client.Socket.GetRemoteAddress()} {client.Name} State: {client.State} Bytes: {Terraria.NetMessage.buffer[client.Id].totalData}");
                args.Player.SendInfoMessage($"Status: {client.StatusText}");
                args.Player.SendInfoMessage($"Status: {client.StatusText2}");
            }
        }
    }

    private void Command_DumpBuffer(CommandArgs args)
    {
        if (args.Parameters.Count == 0)
        {
            args.Player.SendErrorMessage("Invalid player.");
            return;
        }

        if (!byte.TryParse(args.Parameters[0], out var index))
        {
            args.Player.SendErrorMessage("Invalid player.");
            return;
        }

        var path = args.Parameters.Count > 1 ? string.Join("_", args.Parameters[1].Split(Path.GetInvalidFileNameChars())) : "dump.bin";
        path = Path.Combine(TShockAPI.TShock.SavePath, path);

        File.WriteAllBytes(path, Terraria.NetMessage.buffer[index].readBuffer[..Terraria.NetMessage.buffer[index].totalData]);
    }

    private void Command_TerminateSocket(CommandArgs args)
    {
        if (args.Parameters.Count == 0)
        {
            args.Player.SendErrorMessage("Invalid player.");
            return;
        }

        if (!byte.TryParse(args.Parameters[0], out var index))
        {
            args.Player.SendErrorMessage("Invalid player.");
            return;
        }

        Terraria.Netplay.Clients[index]?.Socket?.Close();
    }

    private class DummyGroup : Group
    {
        public DummyGroup() : base("*dummy*")
        {
        }

        public override bool HasPermission(string permission)
        {
            return permission != TShockAPI.Permissions.bypassssc;
        }
    }

    private class DummyPlayer : TSPlayer
    {
        public DummyPlayer() : base("Dummy")
        {
            this.Group = new DummyGroup();
            this.IsLoggedIn = true;
            this.Account = new TShockAPI.DB.UserAccount();
        }
    }

    private void Command_ResetCharacter(CommandArgs args)
    {
        var account = new List<int>();
        if (args.Parameters.Count == 0 || (args.Parameters.Count == 1 && args.Parameters[0] == "-f"))
        {
            if (args.Player.Account?.ID != null)
            {
                account.Add(args.Player.Account.ID);
            }
        }
        else if (args.Parameters[0] == "*" && args.Player.HasPermission(Consts.Permissions.Admin.ResetCharacterAll))
        {
            account = TShockAPI.TShock.UserAccounts.GetUserAccounts().Select(a => a.ID).ToList();
        }
        else if ((args.Parameters[0].StartsWith("tsp:") || args.Parameters[0].StartsWith("tsi:"))
            && args.Player.HasPermission(Consts.Permissions.Admin.ResetCharacterOther))
        {
            var ta = TSPlayer.FindByNameOrID(args.Parameters[0]).SingleOrDefault();
            if (ta != null && ta.Account != null)
            {
                account.Add(ta.Account.ID);
            }
        }
        else if ((args.Parameters[0].StartsWith("usr:") || args.Parameters[0].StartsWith("usi:"))
            && args.Player.HasPermission(Consts.Permissions.Admin.ResetCharacterOther))
        {
            account = Utils.SearchUserAccounts(args.Parameters[0]).Select(a => a.ID).ToList();
        }

        if (account.Count == 0)
        {
            args.Player.SendErrorMessage("Invalid player.");
            return;
        }

        if (!args.Parameters.Contains("-f"))
        {
            if (account.Count > 20)
            {
                args.Player.SendErrorMessage("More than 20 players will be reset. Use -f to force.");
                return;
            }
            if (account.Count > 1)
            {
                var names = string.Join(", ", account.Select(a =>
                {
                    var acc = TShockAPI.TShock.UserAccounts.GetUserAccountByID(a);
                    return $"{acc.Name} (Index: {acc.ID})";
                }).ToArray());
                args.Player.SendErrorMessage($"{names} will be reset. Use -f to force.");
                return;
            }
            if (args.Player.Account?.ID != null && account.Contains(args.Player.Account.ID))
            {
                args.Player.SendErrorMessage("Your character data will be reset. Use -f to force.");
                return;
            }
        }

        var resetStyle = args.Parameters.Contains("-s");
        foreach (var a in account)
        {
            var p = new DummyPlayer();
            p.Account.ID = a;
            var data = new PlayerData(p);
            var existing = TShockAPI.TShock.CharacterDB.GetPlayerData(p, a);
            if (!resetStyle)
            {
                data.skinVariant = existing.skinVariant;
                data.hair = existing.hair;
                data.hairDye = existing.hairDye;
                data.hairColor = existing.hairColor;
                data.pantsColor = existing.pantsColor;
                data.shirtColor = existing.shirtColor;
                data.underShirtColor = existing.underShirtColor;
                data.shoeColor = existing.shoeColor;
                data.hideVisuals = existing.hideVisuals;
                data.skinColor = existing.skinColor;
                data.eyeColor = existing.eyeColor;
            }
            var ps = TShockAPI.TShock.ServerSideCharacterConfig.Settings;
            data.health = ps.StartingHealth;
            data.maxHealth = ps.StartingHealth;
            data.mana = ps.StartingMana;
            data.maxMana = ps.StartingMana;
            data.spawnX = -1;
            data.spawnY = -1;
            data.questsCompleted = 0;
            data.inventory = new NetItem[NetItem.MaxInventory];
            for (var i = 0; i < ps.StartingInventory.Count; i++)
            {
                data.inventory[i] = ps.StartingInventory[i];
            }
            foreach (var client in TShockAPI.TShock.Players)
            {
                if (client == null || client.Account == null || client.Account.ID != a)
                {
                    continue;
                }
                client.PlayerData = data;
                data.RestoreCharacter(client);
                break;
            }
            TShockAPI.TShock.CharacterDB.InsertSpecificPlayerData(p, data);
        }
    }
}
