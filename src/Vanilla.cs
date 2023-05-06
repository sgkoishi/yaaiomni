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

        if (!TShockAPI.TShock.Groups.GroupExists(LegacyConsts.VanillaGroup))
        {
            TShockAPI.TShock.Groups.AddGroup(LegacyConsts.VanillaGroup, null, "", "255,255,255");
        }

        var addperm = TShockAPI.TShock.Groups.AddPermissions(LegacyConsts.VanillaGroup, vanillaMode.Permissions);
        if (vanillaMode.AllowJourney)
        {
            addperm += TShockAPI.TShock.Groups.AddPermissions(LegacyConsts.VanillaGroup, new List<string> { "tshock.journey.*" });
        }
        if (vanillaMode.IgnoreAntiCheat)
        {
            addperm += TShockAPI.TShock.Groups.AddPermissions(LegacyConsts.VanillaGroup, new List<string> { "tshock.ignore.*", "!tshock.ignore.ssc" });
        }
        if (addperm.Length > 0)
        {
            TShockAPI.TSPlayer.Server.SendInfoMessage($"Failed to add permissions to group {LegacyConsts.VanillaGroup}.");
        }

        var vg = TShockAPI.TShock.Groups.GetGroupByName(LegacyConsts.VanillaGroup);
        TShockAPI.TShock.Groups.UpdateGroup(LegacyConsts.VanillaGroup, null, vg.Permissions, vg.ChatColor, vg.Suffix, vg.Prefix);
        var group = TShockAPI.TShock.Groups.GetGroupByName(TShockAPI.TShock.Config.Settings.DefaultRegistrationGroupName);
        group = Utils.ParentGroup(group, _ => true);
        if (group == null)
        {
            TShockAPI.TSPlayer.Server.SendErrorMessage($"Failed to find group {TShockAPI.TShock.Config.Settings.DefaultRegistrationGroupName}.");
            return;
        }
        TShockAPI.TShock.Groups.UpdateGroup(group.Name, LegacyConsts.VanillaGroup, group.Permissions, group.ChatColor, group.Suffix, group.Prefix);
    }

    private void PermissionSetup()
    {
        var preset = this.config.Permission.Preset;
        var vanillaMode = this.config.Mode.Vanilla.Enabled;
        if (!preset.Enabled && !vanillaMode && !preset.AlwaysApply)
        {
            return;
        }

        if (File.Exists(Path.Combine(TShockAPI.TShock.SavePath, LegacyConsts.PresetLock)) && !preset.AlwaysApply)
        {
            return;
        }

        var guest = TShockAPI.TShock.Groups.GetGroupByName(TShockAPI.TShock.Config.Settings.DefaultGuestGroupName);
        if (preset.AllowRestricted || vanillaMode)
        {
            AddPermission(guest,
                LegacyConsts.Permissions.TogglePvP,
                LegacyConsts.Permissions.ToggleTeam,
                LegacyConsts.Permissions.SyncLoadout,
                LegacyConsts.Permissions.PvPCommand,
                LegacyConsts.Permissions.TeamCommand);
        }
        AddPermission(guest, LegacyConsts.Permissions.Ping);
        AddPermission(guest, LegacyConsts.Permissions.Echo);

        AliasPermission(TShockAPI.Permissions.canchat, LegacyConsts.Permissions.Chat);
        AliasPermission(LegacyConsts.Permissions.TogglePvP, $"{LegacyConsts.Permissions.TogglePvP}.*");
        AliasPermission(LegacyConsts.Permissions.ToggleTeam, $"{LegacyConsts.Permissions.ToggleTeam}.*");
        AliasPermission(TShockAPI.Permissions.summonboss, $"{LegacyConsts.Permissions.SummonBoss}.*");
        AliasPermission(TShockAPI.Permissions.startinvasion, $"{LegacyConsts.Permissions.SummonBoss}.*");

        if (preset.DebugForAdminOnly)
        {
            AliasPermission(TShockAPI.Permissions.kick, PluginConsts.Permissions.Whynot);
        }
        else
        {
            AddPermission(guest, PluginConsts.Permissions.Whynot);
        }

        AliasPermission(TShockAPI.Permissions.kick,
            LegacyConsts.Permissions.Admin.Ghost,
            LegacyConsts.Permissions.Admin.SetLanguage,
            LegacyConsts.Permissions.Admin.DebugStat,
            LegacyConsts.Permissions.Admin.SetPvp,
            LegacyConsts.Permissions.Admin.SetTeam,
            LegacyConsts.Permissions.TimeoutCommand,
            LegacyConsts.Permissions.IntervalCommand,
            LegacyConsts.Permissions.ClearInterval,
            LegacyConsts.Permissions.ShowTimeout,
            LegacyConsts.Permissions.ResetCharacter);

        AliasPermission(TShockAPI.Permissions.maintenance,
            LegacyConsts.Permissions.Admin.MaxPlayers,
            LegacyConsts.Permissions.Admin.TileProvider,
            LegacyConsts.Permissions.Admin.TriggerGarbageCollection,
            LegacyConsts.Permissions.Admin.RawBroadcast,
            LegacyConsts.Permissions.Admin.TerminateSocket,
            LegacyConsts.Permissions.Admin.ResetCharacterOther,
            LegacyConsts.Permissions.Admin.ExportCharacter);

        AliasPermission(TShockAPI.Permissions.su,
            LegacyConsts.Permissions.Admin.Sudo,
            LegacyConsts.Permissions.Admin.ListClients,
            LegacyConsts.Permissions.Admin.DumpBuffer,
            LegacyConsts.Permissions.Admin.ResetCharacterAll);

        File.WriteAllText(Path.Combine(TShockAPI.TShock.SavePath, LegacyConsts.PresetLock), string.Empty);
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