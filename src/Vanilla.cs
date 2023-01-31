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
            guest?.AddPermission(Consts.Permissions.TogglePvP);
            guest?.AddPermission(Consts.Permissions.ToggleTeam);
            guest?.AddPermission(Consts.Permissions.SyncLoadout);
        }
        guest?.AddPermission(Consts.Permissions.Ping);

        this.AliasPermission(TShockAPI.Permissions.canchat, Consts.Permissions.Chat);
        this.AliasPermission(TShockAPI.Permissions.summonboss, $"{Consts.Permissions.SummonBoss}.*");
        this.AliasPermission(TShockAPI.Permissions.startinvasion, $"{Consts.Permissions.SummonBoss}.*");

        var na = Utils.ParentGroup(
            TShockAPI.TShock.Groups.GetGroupByName("owner") ?? TShockAPI.TShock.Groups.GetGroupByName("newadmin"),
            g => g.HasPermission(TShockAPI.Permissions.kick));

        na?.AddPermission(Consts.Permissions.Admin.Ghost);
        na?.AddPermission(Consts.Permissions.Admin.SetLanguage);
        na?.AddPermission(Consts.Permissions.Admin.DebugStat);
        na?.AddPermission(Consts.Permissions.Admin.SetPvp);
        na?.AddPermission(Consts.Permissions.Admin.SetTeam);
        na?.AddPermission(Consts.Permissions.TimeoutCommand);
        na?.AddPermission(Consts.Permissions.IntervalCommand);
        na?.AddPermission(Consts.Permissions.ClearInterval);
        na?.AddPermission(Consts.Permissions.ShowTimeout);
        (preset.DebugForAdminOnly ? na : guest)?.AddPermission(Consts.Permissions.Whynot);

        var ta = Utils.ParentGroup(
            TShockAPI.TShock.Groups.GetGroupByName("owner") ?? TShockAPI.TShock.Groups.GetGroupByName("trustedadmin"),
            g => g.HasPermission(TShockAPI.Permissions.maintenance));

        ta?.AddPermission(Consts.Permissions.Admin.MaxPlayers);
        ta?.AddPermission(Consts.Permissions.Admin.TileProvider);
        ta?.AddPermission(Consts.Permissions.Admin.TriggerGarbageCollection);
        ta?.AddPermission(Consts.Permissions.Admin.RawBroadcast);
        ta?.AddPermission(Consts.Permissions.Admin.TerminateSocket);
        ta?.AddPermission(Consts.Permissions.Admin.ResetCharacterOther);
        ta?.AddPermission(Consts.Permissions.Admin.ExportCharacter);

        var owner = Utils.ParentGroup(
            TShockAPI.TShock.Groups.GetGroupByName("owner"),
            g => g.HasPermission(TShockAPI.Permissions.su));

        owner?.AddPermission(Consts.Permissions.Admin.Sudo);
        owner?.AddPermission(Consts.Permissions.Admin.ListClients);
        owner?.AddPermission(Consts.Permissions.Admin.DumpBuffer);
        owner?.AddPermission(Consts.Permissions.Admin.ResetCharacterAll);

        File.WriteAllText(Path.Combine(TShockAPI.TShock.SavePath, Consts.PresetLock), string.Empty);
    }

    private void AliasPermission(string orig, string equiv)
    {
        foreach (var group in TShockAPI.TShock.Groups.groups)
        {
            if (group.HasPermission(orig))
            {
                group.AddPermission(equiv);
            }
        }
    }
}
