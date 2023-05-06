using Chireiden.TShock.Omni.DefinedConsts;
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

        if (!TShockAPI.TShock.Groups.GroupExists(Misc.VanillaGroup))
        {
            TShockAPI.TShock.Groups.AddGroup(Misc.VanillaGroup, null, "", "255,255,255");
        }

        var addperm = TShockAPI.TShock.Groups.AddPermissions(Misc.VanillaGroup, vanillaMode.Permissions);
        if (vanillaMode.AllowJourney)
        {
            addperm += TShockAPI.TShock.Groups.AddPermissions(Misc.VanillaGroup, new List<string> { "tshock.journey.*" });
        }
        if (vanillaMode.IgnoreAntiCheat)
        {
            addperm += TShockAPI.TShock.Groups.AddPermissions(Misc.VanillaGroup, new List<string> { "tshock.ignore.*", "!tshock.ignore.ssc" });
        }
        if (addperm.Length > 0)
        {
            TShockAPI.TSPlayer.Server.SendInfoMessage($"Failed to add permissions to group {Misc.VanillaGroup}.");
        }

        var vg = TShockAPI.TShock.Groups.GetGroupByName(Misc.VanillaGroup);
        TShockAPI.TShock.Groups.UpdateGroup(Misc.VanillaGroup, null, vg.Permissions, vg.ChatColor, vg.Suffix, vg.Prefix);
        var group = TShockAPI.TShock.Groups.GetGroupByName(TShockAPI.TShock.Config.Settings.DefaultRegistrationGroupName);
        group = Utils.ParentGroup(group, _ => true);
        if (group == null)
        {
            TShockAPI.TSPlayer.Server.SendErrorMessage($"Failed to find group {TShockAPI.TShock.Config.Settings.DefaultRegistrationGroupName}.");
            return;
        }
        TShockAPI.TShock.Groups.UpdateGroup(group.Name, Misc.VanillaGroup, group.Permissions, group.ChatColor, group.Suffix, group.Prefix);
    }

    private void PermissionSetup()
    {
        var preset = this.config.Permission.Preset;
        var vanillaMode = this.config.Mode.Vanilla.Enabled;
        if (!preset.Enabled && !vanillaMode && !preset.AlwaysApply)
        {
            return;
        }

        if (File.Exists(Path.Combine(TShockAPI.TShock.SavePath, Misc.PresetLock)) && !preset.AlwaysApply)
        {
            return;
        }

        var guest = TShockAPI.TShock.Groups.GetGroupByName(TShockAPI.TShock.Config.Settings.DefaultGuestGroupName);
        if (preset.AllowRestricted || vanillaMode)
        {
            AddPermission(guest,
                Permissions.TogglePvP,
                Permissions.ToggleTeam,
                Permissions.SyncLoadout,
                Permissions.PvPStatus,
                Permissions.TeamStatus);
        }

        AddPermission(guest, Permissions.Ping);
        AddPermission(guest, Permissions.Echo);

        AliasPermission(TShockAPI.Permissions.canchat, Permissions.Chat);
        AliasPermission(Permissions.TogglePvP, $"{Permissions.TogglePvP}.*");
        AliasPermission(Permissions.ToggleTeam, $"{Permissions.ToggleTeam}.*");
        AliasPermission(TShockAPI.Permissions.summonboss, $"{Permissions.SummonBoss}.*");
        AliasPermission(TShockAPI.Permissions.startinvasion, $"{Permissions.SummonBoss}.*");

        if (preset.DebugForAdminOnly)
        {
            AliasPermission(TShockAPI.Permissions.kick, Permissions.Whynot);
        }
        else
        {
            AddPermission(guest, Permissions.Whynot);
        }

        AliasPermission(TShockAPI.Permissions.kick,
            Permissions.Admin.Ghost,
            Permissions.Admin.ManageLanguage,
            Permissions.Admin.DebugStat,
            Permissions.Admin.PvPStatus,
            Permissions.Admin.TeamStatus,
            Permissions.SetTimeout,
            Permissions.SetInterval,
            Permissions.ClearInterval,
            Permissions.ShowTimeout,
            Permissions.ResetCharacter);

        AliasPermission(TShockAPI.Permissions.maintenance,
            Permissions.Admin.MaxPlayers,
            Permissions.Admin.TileProvider,
            Permissions.Admin.GarbageCollect,
            Permissions.Admin.RawBroadcast,
            Permissions.Admin.TerminateSocket,
            Permissions.Admin.ResetCharacterOther,
            Permissions.Admin.ExportCharacter);

        AliasPermission(TShockAPI.Permissions.su,
            Permissions.Admin.Sudo,
            Permissions.Admin.ListClients,
            Permissions.Admin.DumpBuffer,
            Permissions.Admin.ResetCharacterAll);

        File.WriteAllText(Path.Combine(TShockAPI.TShock.SavePath, Misc.PresetLock), string.Empty);
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