using TerrariaApi.Server;

namespace Chireiden.TShock.Omni;

public partial class Plugin : TerrariaPlugin
{
    private void VanillaSetup()
    {
        if (this.config.Mode.Vanilla.Enabled)
        {
            const string vanilla = "chireiden_vanilla";
            if (!TShockAPI.TShock.Groups.GroupExists(vanilla))
            {
                TShockAPI.TShock.Groups.AddGroup(vanilla, null, "", "255,255,255");
            }
            var addperm = TShockAPI.TShock.Groups.AddPermissions(vanilla, this.config.Mode.Vanilla.Permissions.ToList());
            if (this.config.Mode.Vanilla.AllowJourney)
            {
                addperm += TShockAPI.TShock.Groups.AddPermissions(vanilla, new List<string> { "tshock.journey.*" });
            }
            if (this.config.Mode.Vanilla.IgnoreAntiCheat)
            {
                addperm += TShockAPI.TShock.Groups.AddPermissions(vanilla, new List<string> { "tshock.ignore.*", "!tshock.ignore.ssc" });
            }
            if (addperm.Length > 0)
            {
                TShockAPI.TSPlayer.Server.SendInfoMessage($"Failed to add permissions to group chireiden_vanilla.");
            }
            var vg = TShockAPI.TShock.Groups.GetGroupByName(vanilla);
            TShockAPI.TShock.Groups.UpdateGroup(vanilla, null, vg.Permissions, vg.ChatColor, vg.Suffix, vg.Prefix);
            var group = TShockAPI.TShock.Groups.GetGroupByName(TShockAPI.TShock.Config.Settings.DefaultRegistrationGroupName);
            while (group.Parent != null)
            {
                group = group.Parent;
            }
            TShockAPI.TShock.Groups.UpdateGroup(group.Name, vanilla, group.Permissions, group.ChatColor, group.Suffix, group.Prefix);
        }
    }
}
