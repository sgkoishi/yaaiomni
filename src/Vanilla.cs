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
        while (group.Parent != null)
        {
            group = group.Parent;
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

        if (preset.Restrict)
        {
            var guest = TShockAPI.TShock.Groups.GetGroupByName("guest");
            if (!guest.HasPermission(Consts.Permissions.TogglePvP))
            {
                guest.AddPermission(Consts.Permissions.TogglePvP);
            }
            if (!guest.HasPermission(Consts.Permissions.ToggleTeam))
            {
                guest.AddPermission(Consts.Permissions.ToggleTeam);
            }
        }

        var na = TShockAPI.TShock.Groups.GetGroupByName("newadmin");
        if (!na.HasPermission(Consts.Permissions.Ghost))
        {
            na.AddPermission(Consts.Permissions.Ghost);
        }
        if (!na.HasPermission(Consts.Permissions.SetLanguage))
        {
            na.AddPermission(Consts.Permissions.SetLanguage);
        }
        if (preset.DebugForAdmin)
        {
            if (!na.HasPermission(Consts.Permissions.Whynot))
            {
                na.AddPermission(Consts.Permissions.Whynot);
            }
        }
    }
}
