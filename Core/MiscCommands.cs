using System.Reflection;
using TShockAPI;

namespace Chireiden.TShock.Omni;

public partial class Plugin
{
    [Command("Admin.MaxPlayers", "maxplayers", Permission = "chireiden.omni.admin.maxplayers")]
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

    [Command("Admin.Sudo", "runas", Permission = "chireiden.omni.admin.sudo")]
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

        var withoutcheck = args.Parameters.Count == 3 && args.Parameters.Contains("-f");
        var parm = args.Parameters.Except(new string[] { "-f" }).ToArray();

        var player = TSPlayer.FindByNameOrID(parm[0]);
        if (player.Count == 1)
        {
            // Right one
        }
        else if (parm[0] == "*" || this.config.PlayerWildcardFormat.Value.Contains(parm[0]))
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

        if (withoutcheck)
        {
            foreach (var p in player)
            {
                this.RunWithoutPermissionChecks(() => TShockAPI.Commands.HandleCommand(p, parm[1]), p);
            }
        }
        else
        {
            foreach (var p in player)
            {
                TShockAPI.Commands.HandleCommand(p, parm[1]);
            }
        }
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

    private class DummyTSPlayer : TSPlayer
    {
        public DummyTSPlayer() : base("Dummy")
        {
            this.Group = new DummyGroup();
            this.IsLoggedIn = true;
            this.Account = new TShockAPI.DB.UserAccount();
            // Do not sync it to actual players
            this.Index = int.MaxValue;
        }
        public Terraria.Player Player
        {
            get => this.TPlayer;
            set => typeof(TSPlayer).GetField("FakePlayer", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(this, value);
        }
    }

    [Command("ResetCharacter", "resetcharacter", Permission = "chireiden.omni.resetcharacter")]
    [RelatedPermission("Admin.ResetCharacterOther", "chireiden.omni.admin.resetcharacter")]
    [RelatedPermission("Admin.ResetCharacterAll", "chireiden.omni.admin.resetcharacter.all")]
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
        else if (args.Parameters[0] == "*")
        {
            if (args.Player.HasPermission(DefinedConsts.PermissionsList.Admin.ResetCharacterAll))
            {
                account = Utils.SearchUserAccounts(args.Parameters[0]).Select(a => a.ID).ToList();
            }
            else
            {
                args.Player.SendErrorMessage("You do not have permission to reset all players characters.");
            }
        }
        else if (args.Parameters[0].StartsWith("tsp:")
            || args.Parameters[0].StartsWith("tsi:")
            || args.Parameters[0].StartsWith("usr:")
            || args.Parameters[0].StartsWith("usi:"))
        {
            if (args.Player.HasPermission(DefinedConsts.PermissionsList.Admin.ResetCharacterOther))
            {
                account = Utils.SearchUserAccounts(args.Parameters[0]).Select(a => a.ID).ToList();
            }
            else
            {
                args.Player.SendErrorMessage("You do not have permission to reset other players characters.");
            }
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
            var p = new DummyTSPlayer();
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

    [Command("Admin.ExportCharacter", "exportcharacter", Permission = "chireiden.omni.admin.exportcharacter")]
    private void Command_ExportCharacter(CommandArgs args)
    {
        var accounts = args.Parameters.Count == 0
            ? args.Player == TSPlayer.Server
                ? TShockAPI.TShock.UserAccounts.GetUserAccounts()
                : [args.Player.Account]
            : Utils.SearchUserAccounts(args.Parameters[0]).ToList();

        if (accounts.Count == 0)
        {
            args.Player.SendErrorMessage("No accounts found.");
            return;
        }

        var dir = Path.Combine(TShockAPI.TShock.SavePath, "exported");
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        foreach (var account in accounts)
        {
            try
            {
                if (account == null)
                {
                    args.Player.SendErrorMessage("Account not found.");
                    continue;
                }
                var p = new DummyTSPlayer();
                p.Account.ID = account.ID;
                p.Player = new Terraria.Player
                {
                    name = account.Name
                };
                var data = TShockAPI.TShock.CharacterDB.GetPlayerData(p, account.ID);
                try
                {
                    data.RestoreCharacter(p);
                }
                catch
                {
                    // This will throw, failed to sync with dummy player.
                }
                var file = new Terraria.IO.PlayerFileData
                {
                    Metadata = Terraria.IO.FileMetadata.FromCurrentSettings(Terraria.IO.FileType.Player),
                    Player = p.Player,
                    _path = Path.Combine(dir, $"{account.Name}.plr")
                };
                Terraria.Player.InternalSavePlayerFile(file);
                args.Player.SendSuccessMessage($"Exported {account.Name} to {dir}.");
            }
            catch (Exception e)
            {
                TShockAPI.TShock.Log.ConsoleError($"Failed to export {account.Name}: {e.Message}");
                args.Player.SendErrorMessage($"Failed to export {account.Name}.");
            }
        }
    }

    [Command("Echo", "echo", Permission = "chireiden.omni.echo")]
    private void Command_Echo(CommandArgs args)
    {
        args.Player.SendInfoMessage(string.Join(" ", args.Parameters));
    }

    [Command("Admin.ApplyDefaultPermission", "_setperm", Permission = "chireiden.omni.admin.setupperm")]
    private void Command_SetupPermission(CommandArgs args)
    {
        this.PermissionSetup();
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

    [Command("Admin.RunBackground", "_qbg", Permission = "chireiden.omni.admin.runbackground")]
    private void Command_RunBackground(CommandArgs args)
    {
        if (args.Parameters.Count == 0)
        {
            args.Player.SendErrorMessage("No command given.");
            return;
        }
        if (args.Parameters[0] == "-t" && args.Parameters.Count > 1)
        {
            Task.Run(() => TShockAPI.Commands.HandleCommand(args.Player, args.Parameters[1]));
            args.Player.SendSuccessMessage($"Background task ({args.Player.Name} @ {args.Parameters[1]}) started.");
            return;
        }

        System.Threading.ThreadPool.QueueUserWorkItem(_ => TShockAPI.Commands.HandleCommand(args.Player, args.Parameters[0]));
        args.Player.SendSuccessMessage($"Background task ({args.Player.Name} @ {args.Parameters[0]}) queued.");
    }
}