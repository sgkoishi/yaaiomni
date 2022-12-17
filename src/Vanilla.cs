using TerrariaApi.Server;

namespace Chireiden.TShock.Omni;

public partial class Plugin : TerrariaPlugin
{
    private void VanillaSetup()
    {
        if (this.config.Mode.Vanilla.Enabled)
        {
            if (!TShockAPI.TShock.Groups.GroupExists(Consts.VanillaGroup))
            {
                TShockAPI.TShock.Groups.AddGroup(Consts.VanillaGroup, null, "", "255,255,255");
            }
            var addperm = TShockAPI.TShock.Groups.AddPermissions(Consts.VanillaGroup, this.config.Mode.Vanilla.Permissions);
            if (this.config.Mode.Vanilla.AllowJourney)
            {
                addperm += TShockAPI.TShock.Groups.AddPermissions(Consts.VanillaGroup, new List<string> { "tshock.journey.*" });
            }
            if (this.config.Mode.Vanilla.IgnoreAntiCheat)
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
    }

    private void PermissionSetup()
    {
        var na = TShockAPI.TShock.Groups.GetGroupByName("newadmin");
        if (!na.HasPermission(Consts.Permissions.Ghost))
        {
            na.AddPermission(Consts.Permissions.Ghost);
        }
        if (!na.HasPermission(Consts.Permissions.SetLanguage))
        {
            na.AddPermission(Consts.Permissions.SetLanguage);
        }
        if (this.config.Permission.Preset.DebugForAdmin)
        {
            if (!na.HasPermission(Consts.Permissions.Whynot))
            {
                na.AddPermission(Consts.Permissions.Whynot);
            }
        }
    }
}
