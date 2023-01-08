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
            TShockAPI.TSPlayer.Server.SendInfoMessage($"Failed to add permissions to group chireiden_vanilla.");
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
        if (!preset.Enabled && !vanillaMode)
        {
            return;
        }

        var guest = TShockAPI.TShock.Groups.GetGroupByName(TShockAPI.TShock.Config.Settings.DefaultGuestGroupName);
        if (preset.Restrict || vanillaMode)
        {
            guest?.AddPermission(Consts.Permissions.TogglePvP);
            guest?.AddPermission(Consts.Permissions.ToggleTeam);
            guest?.AddPermission(Consts.Permissions.SyncLoadout);
        }

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

        var owner = Utils.ParentGroup(
            TShockAPI.TShock.Groups.GetGroupByName("owner"),
            g => g.HasPermission(TShockAPI.Permissions.su));

        owner?.AddPermission(Consts.Permissions.Admin.Sudo);
    }
}
