﻿using Chireiden.TShock.Omni.DefinedConsts;

namespace Chireiden.TShock.Omni;

public partial class Plugin
{
    private void VanillaSetup()
    {
        var vanillaMode = this.config.Mode.Value.Vanilla.Value;
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
        if (addperm.Length == 0)
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
        if (group.Name != Misc.VanillaGroup)
        {
            try
            {
                TShockAPI.TShock.Groups.UpdateGroup(group.Name, Misc.VanillaGroup, group.Permissions, group.ChatColor, group.Suffix, group.Prefix);
            }
            catch (Exception e)
            {
                TShockAPI.TSPlayer.Server.SendInfoMessage($"Failed to set parent group to {Misc.VanillaGroup}: {e}");
            }
        }
    }

    private void DefaultPermissionSetup()
    {
        var preset = this.config.Permission.Value.Preset.Value;
        var vanillaMode = this.config.Mode.Value.Vanilla.Value.Enabled;
        if (!preset.Enabled && !vanillaMode && !preset.AlwaysApply)
        {
            return;
        }

        if (File.Exists(Path.Combine(TShockAPI.TShock.SavePath, Misc.PresetLock)) && !preset.AlwaysApply)
        {
            return;
        }

        this.PermissionSetup();
    }

    private void PermissionSetup()
    {
        var preset = this.config.Permission.Value.Preset.Value;
        var vanillaMode = this.config.Mode.Value.Vanilla.Value.Enabled;
        var guest = TShockAPI.TShock.Groups.GetGroupByName(TShockAPI.TShock.Config.Settings.DefaultGuestGroupName);
        if (preset.AllowRestricted || vanillaMode)
        {
            AddPermission(guest,
                Permission.TogglePvP,
                Permission.ToggleTeam,
                Permission.SyncLoadout,
                Permission.PvPStatus,
                Permission.TeamStatus);
        }

        AddPermission(guest, Permission.Ping);
        AddPermission(guest, Permission.Echo);

        AliasPermission(TShockAPI.Permissions.canchat, Permission.Chat);
        AliasPermission(Permission.TogglePvP, $"{Permission.TogglePvP}.*");
        AliasPermission(Permission.ToggleTeam, $"{Permission.ToggleTeam}.*");
        AliasPermission(TShockAPI.Permissions.summonboss, $"{Permission.SummonBoss}.*");
        AliasPermission(TShockAPI.Permissions.startinvasion, $"{Permission.SummonBoss}.*");

        if (preset.DebugForAdminOnly)
        {
            AliasPermission(TShockAPI.Permissions.kick, Permission.Whynot);
        }
        else
        {
            AddPermission(guest, Permission.Whynot);
        }

        AliasPermission(TShockAPI.Permissions.kick,
            Permission.Admin.Ghost,
            Permission.Admin.ManageLanguage,
            Permission.Admin.DebugStat,
            Permission.Admin.PvPStatus,
            Permission.Admin.TeamStatus,
            Permission.Admin.UpsCheck,
            Permission.SetTimeout,
            Permission.SetInterval,
            Permission.ClearInterval,
            Permission.ShowTimeout,
            Permission.ResetCharacter);

        AliasPermission(TShockAPI.Permissions.maintenance,
            Permission.Admin.MaxPlayers,
            Permission.Admin.TileProvider,
            Permission.Admin.GarbageCollect,
            Permission.Admin.RawBroadcast,
            Permission.Admin.TerminateSocket,
            Permission.Admin.ResetCharacterOther,
            Permission.Admin.ExportCharacter,
            Permission.Admin.ApplyDefaultPermission,
            Permission.Admin.GenerateFullConfig);

        AliasPermission(TShockAPI.Permissions.su,
            Permission.Admin.Sudo,
            Permission.Admin.ListClients,
            Permission.Admin.DumpBuffer,
            Permission.Admin.ResetCharacterAll);

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