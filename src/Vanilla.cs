using TerrariaApi.Server;

namespace Chireiden.TShock.Omni;

public partial class Plugin : TerrariaPlugin
{
    private void VanillaSetup()
    {
        var vanillaMode = this.config.Mode.Vanilla;
        if (!vanillaMode.Enabled)
        {
            return;
        }

        if (!TShockAPI.TShock.Groups.GroupExists(Consts.VanillaGroup))
        {
            TShockAPI.TShock.Groups.AddGroup(Consts.VanillaGroup, null, "", "255,255,255");
        }

        var addperm = TShockAPI.TShock.Groups.AddPermissions(Consts.VanillaGroup, vanillaMode.Permissions);
        if (vanillaMode.AllowJourney)
        {
            addperm += TShockAPI.TShock.Groups.AddPermissions(Consts.VanillaGroup, new List<string> { "tshock.journey.*" });
        }
        if (vanillaMode.IgnoreAntiCheat)
        {
            addperm += TShockAPI.TShock.Groups.AddPermissions(Consts.VanillaGroup, new List<string> { "tshock.ignore.*", "!tshock.ignore.ssc" });
        }
        if (addperm.Length > 0)
        {
            TShockAPI.TSPlayer.Server.SendInfoMessage($"Failed to add permissions to group {Consts.VanillaGroup}.");
        }

        var vg = TShockAPI.TShock.Groups.GetGroupByName(Consts.VanillaGroup);
        TShockAPI.TShock.Groups.UpdateGroup(Consts.VanillaGroup, null, vg.Permissions, vg.ChatColor, vg.Suffix, vg.Prefix);
        var group = TShockAPI.TShock.Groups.GetGroupByName(TShockAPI.TShock.Config.Settings.DefaultRegistrationGroupName);
        group = Utils.ParentGroup(group, _ => true);
        if (group == null)
        {
            TShockAPI.TSPlayer.Server.SendErrorMessage($"Failed to find group {TShockAPI.TShock.Config.Settings.DefaultRegistrationGroupName}.");
            return;
        }
        TShockAPI.TShock.Groups.UpdateGroup(group.Name, Consts.VanillaGroup, group.Permissions, group.ChatColor, group.Suffix, group.Prefix);
    }

    private void PermissionSetup()
    {
        var preset = this.config.Permission.Preset;
        var vanillaMode = this.config.Mode.Vanilla.Enabled;
        if (!preset.Enabled && !vanillaMode && !preset.AlwaysApply)
        {
            return;
        }

        if (File.Exists(Path.Combine(TShockAPI.TShock.SavePath, Consts.PresetLock)) && !preset.AlwaysApply)
        {
            return;
        }

        var guest = TShockAPI.TShock.Groups.GetGroupByName(TShockAPI.TShock.Config.Settings.DefaultGuestGroupName);
        if (preset.AllowRestricted || vanillaMode)
        {
            AddPermission(guest,
                Consts.Permissions.TogglePvP,
                Consts.Permissions.ToggleTeam,
                Consts.Permissions.SyncLoadout,
                Consts.Permissions.PvPCommand,
                Consts.Permissions.TeamCommand);
        }
        AddPermission(guest, Consts.Permissions.Ping);
        AddPermission(guest, Consts.Permissions.Echo);

        AliasPermission(TShockAPI.Permissions.canchat, Consts.Permissions.Chat);
        AliasPermission(Consts.Permissions.TogglePvP, $"{Consts.Permissions.TogglePvP}.*");
        AliasPermission(Consts.Permissions.ToggleTeam, $"{Consts.Permissions.ToggleTeam}.*");
        AliasPermission(TShockAPI.Permissions.summonboss, $"{Consts.Permissions.SummonBoss}.*");
        AliasPermission(TShockAPI.Permissions.startinvasion, $"{Consts.Permissions.SummonBoss}.*");

        if (preset.DebugForAdminOnly)
        {
            AliasPermission(TShockAPI.Permissions.kick, Consts.Permissions.Whynot);
        }
        else
        {
            AddPermission(guest, Consts.Permissions.Whynot);
        }

        AliasPermission(TShockAPI.Permissions.kick,
            Consts.Permissions.Admin.Ghost,
            Consts.Permissions.Admin.SetLanguage,
            Consts.Permissions.Admin.DebugStat,
            Consts.Permissions.Admin.SetPvp,
            Consts.Permissions.Admin.SetTeam,
            Consts.Permissions.TimeoutCommand,
            Consts.Permissions.IntervalCommand,
            Consts.Permissions.ClearInterval,
            Consts.Permissions.ShowTimeout,
            Consts.Permissions.ResetCharacter);

        AliasPermission(TShockAPI.Permissions.maintenance,
            Consts.Permissions.Admin.MaxPlayers,
            Consts.Permissions.Admin.TileProvider,
            Consts.Permissions.Admin.TriggerGarbageCollection,
            Consts.Permissions.Admin.RawBroadcast,
            Consts.Permissions.Admin.TerminateSocket,
            Consts.Permissions.Admin.ResetCharacterOther,
            Consts.Permissions.Admin.ExportCharacter);

        AliasPermission(TShockAPI.Permissions.su,
            Consts.Permissions.Admin.Sudo,
            Consts.Permissions.Admin.ListClients,
            Consts.Permissions.Admin.DumpBuffer,
            Consts.Permissions.Admin.ResetCharacterAll);

        File.WriteAllText(Path.Combine(TShockAPI.TShock.SavePath, Consts.PresetLock), string.Empty);
    }

    private static void AliasPermission(string orig, params string[] equiv)
    {
        foreach (var group in TShockAPI.TShock.Groups.groups)
        {
            if (group.HasPermission(orig) && (group.Parent?.HasPermission(orig) != true))
            {
                AddPermission(group, equiv);
            }
        }
    }

    private static void AddPermission(TShockAPI.Group? group, params string[] perm)
    {
        if (group == null)
        {
            return;
        }
        TShockAPI.TShock.Groups.AddPermissions(group!.Name, perm.ToList());
    }
}
