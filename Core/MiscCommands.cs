using System.Reflection;
using TShockAPI;

namespace Chireiden.TShock.Omni;

public partial class Plugin
{
    [Command("Admin.GarbageCollect", "_gc", Permission = "chireiden.omni.admin.gc")]
    private void Command_GC(CommandArgs args)
    {
        if (args.Parameters.Contains("-f"))
        {
            System.Runtime.GCSettings.LargeObjectHeapCompactionMode = System.Runtime.GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect();
        }
        else
        {
            GC.Collect(3, GCCollectionMode.Optimized, false);
        }
        args.Player.SendSuccessMessage("GC Triggered.");
    }

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

    [Command("Admin.RawBroadcast", "rbc", "rawbroadcast", Permission = "chireiden.omni.admin.rawbroadcast")]
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

    [Command("Admin.ListClients", "listclients", Permission = "chireiden.omni.admin.listclients")]
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

    [Command("Admin.DumpBuffer", "dumpbuffer", Permission = "chireiden.omni.admin.dumpbuffer")]
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

    [Command("Admin.TerminateSocket", "kc", Permission = "chireiden.omni.admin.terminatesocket")]
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

    private class DummyTSPlayer : TSPlayer
    {
        public DummyTSPlayer() : base("Dummy")
        {
            this.Group = new DummyGroup();
            this.IsLoggedIn = true;
            this.Account = new TShockAPI.DB.UserAccount();
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
            ? new List<TShockAPI.DB.UserAccount> { args.Player.Account }
            : Utils.SearchUserAccounts(args.Parameters[0]).ToList();

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
                data.RestoreCharacter(p);
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

    private (int Tick, DateTime Time) _tickCheck = (-1, DateTime.MinValue);
    [Command("Admin.UpsCheck", "_ups", Permission = "chireiden.omni.admin.upscheck")]
    private void Command_TicksPerSec(CommandArgs args)
    {
        if (this._tickCheck.Tick == -1)
        {
            this._tickCheck = (this._updateCounter, DateTime.Now);
            args.Player.SendInfoMessage("Started tracking ticks per second.");
        }
        else
        {
            var (Tick, Time) = this._tickCheck;
            this._tickCheck = (-1, DateTime.MinValue);
            var diff = this._updateCounter - Tick;
            var time = DateTime.Now - Time;
            args.Player.SendInfoMessage($"{diff} ticks / {time} seconds: {diff / time.TotalSeconds:F2}");
        }
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
}