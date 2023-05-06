namespace Chireiden.TShock.Omni;

[Obsolete("Migrate to source generator")]
public static class LegacyConsts
{
    public static class Permissions
    {
        public static string TogglePvP => "chireiden.omni.togglepvp";
        public static string ToggleTeam => "chireiden.omni.toggleteam";
        public static string PvPCommand => "chireiden.omni.setpvp";
        public static string TeamCommand => "chireiden.omni.setteam";
        public static string SyncLoadout => "chireiden.omni.syncloadout";
        public static string TimeoutCommand => "chireiden.omni.timeout";
        public static string IntervalCommand => "chireiden.omni.interval";
        public static string ClearInterval => "chireiden.omni.cleartimeout";
        public static string ShowTimeout => "chireiden.omni.showtimeout";
        public static string ResetCharacter => "chireiden.omni.resetcharacter";
        public static string Ping => "chireiden.omni.ping";
        public static string Chat => "chireiden.omni.chat";
        public static string SummonBoss => "chireiden.omni.summonboss";
        public static string Echo => "chireiden.omni.echo";
        public static class Admin
        {
            public static string Ghost => "chireiden.omni.ghost";
            public static string SetLanguage => "chireiden.omni.setlang";
            public static string SetPvp => "chireiden.omni.admin.setpvp";
            public static string SetTeam => "chireiden.omni.admin.setteam";
            public static string TriggerGarbageCollection => "chireiden.omni.admin.gc";
            public static string DebugStat => "chireiden.omni.admin.debugstat";
            public static string MaxPlayers => "chireiden.omni.admin.maxplayers";
            public static string TileProvider => "chireiden.omni.admin.tileprovider";
            public static string RawBroadcast => "chireiden.omni.admin.rawbroadcast";
            public static string Sudo => "chireiden.omni.admin.sudo";
            public static string DetailedPermissionStackTrace => "chireiden.omni.whynot.detailed";
            public static string ListClients => "chireiden.omni.admin.listclients";
            public static string DumpBuffer => "chireiden.omni.admin.dumpbuffer";
            public static string TerminateSocket => "chireiden.omni.admin.terminatesocket";
            public static string ResetCharacterOther => "chireiden.omni.admin.resetcharacter";
            public static string ResetCharacterAll => "chireiden.omni.admin.resetcharacter.all";
            public static string ExportCharacter => "chireiden.omni.admin.exportcharacter";
        }
    }
    public static class Commands
    {
        public static string Ghost => "ghost";
        public static string SetLanguage => "setlang";
        public static string SetPvp => "_pvp";
        public static string SetTeam => "_team";
        public static string TriggerGarbageCollection => "_gc";
        public static string DebugStat => "_debugstat";
        public static string MaxPlayers => "maxplayers";
        public static string TileProvider => "tileprovider";
        public static string Timeout => "settimeout";
        public static string Interval => "setinterval";
        public static string ClearInterval => "clearinterval";
        public static string ShowTimeout => "showdelay";
        public static string RawBroadcast => "rbc";
        public static string Sudo => "runas";
        public static string ListClients => "listclients";
        public static string DumpBuffer => "dumpbuffer";
        public static string TerminateSocket => "kc";
        public static string ResetCharacter => "resetcharacter";
        public static string Ping => "_ping";
        public static string Chat => "_chat";
        public static string ExportCharacter => "exportcharacter";
        public static string Echo => "_echo";
    }
    public static class DataKey
    {
        public static string IsPE => "chireiden.data.ispe";
    }
    public static string ConfigFile => "chireiden.omni.json";
    public static string PresetLock => "chireiden.omni.preset.lock";
    public static string VanillaGroup => "chireiden_vanilla";
}