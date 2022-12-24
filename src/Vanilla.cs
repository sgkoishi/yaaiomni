using TerrariaApi.Server;
using TShockAPI;

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
        group = Parent(group, _ => true);
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
        if (!preset.Enabled)
        {
            return;
        }

        var guest = TShockAPI.TShock.Groups.GetGroupByName(TShockAPI.TShock.Config.Settings.DefaultGuestGroupName);
        if (preset.Restrict)
        {
            guest?.AddPermission(Consts.Permissions.TogglePvP);
            guest?.AddPermission(Consts.Permissions.ToggleTeam);
        }

        var na = TShockAPI.TShock.Groups.GetGroupByName("owner") ?? TShockAPI.TShock.Groups.GetGroupByName("newadmin");
        na = Parent(na, g => g.HasPermission(TShockAPI.Permissions.kick));

        na?.AddPermission(Consts.Permissions.Admin.Ghost);
        na?.AddPermission(Consts.Permissions.Admin.SetLanguage);
        na?.AddPermission(Consts.Permissions.Admin.DebugStat);
        (preset.DebugForAdminOnly ? na : guest)?.AddPermission(Consts.Permissions.Whynot);
    }

    private static Group? Parent(Group? group, Func<Group, bool> predicate)
    {
        var hashset = new HashSet<string>();
        if (group == null || !predicate(group))
        {
            return null;
        }
        while (true)
        {
            if (!hashset.Add(group.Name))
            {
                return null;
            }

            var parent = group.Parent;
            if (parent == null || !predicate(parent))
            {
                return group;
            }
            group = parent;
        }
    }
}
