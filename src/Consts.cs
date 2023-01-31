namespace Chireiden.TShock.Omni;

public static class Consts
{
    public static class Permissions
    {
        public static readonly string Whynot = "chireiden.omni.whynot";
        public static readonly string TogglePvP = "chireiden.omni.togglepvp";
        public static readonly string ToggleTeam = "chireiden.omni.toggleteam";
        public static readonly string PvPCommand = "chireiden.omni.setpvp";
        public static readonly string TeamCommand = "chireiden.omni.setteam";
        public static readonly string SyncLoadout = "chireiden.omni.syncloadout";
        public static readonly string TimeoutCommand = "chireiden.omni.timeout";
        public static readonly string IntervalCommand = "chireiden.omni.interval";
        public static readonly string ClearInterval = "chireiden.omni.cleartimeout";
        public static readonly string ShowTimeout = "chireiden.omni.showtimeout";
        public static readonly string ResetCharacter = "chireiden.omni.resetcharacter";
        public static readonly string Ping = "chireiden.omni.ping";
        public static readonly string Chat = "chireiden.omni.chat";
        public static readonly string SummonBoss = "chireiden.omni.summonboss";
        public static class Admin
        {
            public static readonly string Ghost = "chireiden.omni.ghost";
            public static readonly string SetLanguage = "chireiden.omni.setlang";
            public static readonly string SetPvp = "chireiden.omni.admin.setpvp";
            public static readonly string SetTeam = "chireiden.omni.admin.setteam";
            public static readonly string TriggerGarbageCollection = "chireiden.omni.admin.gc";
            public static readonly string DebugStat = "chireiden.omni.admin.debugstat";
            public static readonly string MaxPlayers = "chireiden.omni.admin.maxplayers";
            public static readonly string TileProvider = "chireiden.omni.admin.tileprovider";
            public static readonly string RawBroadcast = "chireiden.omni.admin.rawbroadcast";
            public static readonly string Sudo = "chireiden.omni.admin.sudo";
            public static readonly string DetailedPermissionStackTrace = "chireiden.omni.whynot.detailed";
            public static readonly string ListClients = "chireiden.omni.admin.listclients";
            public static readonly string DumpBuffer = "chireiden.omni.admin.dumpbuffer";
            public static readonly string TerminateSocket = "chireiden.omni.admin.terminatesocket";
            public static readonly string ResetCharacterOther = "chireiden.omni.admin.resetcharacter";
            public static readonly string ResetCharacterAll = "chireiden.omni.admin.resetcharacter.all";
            public static readonly string ExportCharacter = "chireiden.omni.admin.exportcharacter";
        }
    }
    public static class Commands
    {
        public static readonly string Whynot = "whynot";
        public static readonly string Ghost = "ghost";
        public static readonly string SetLanguage = "setlang";
        public static readonly string SetPvp = "_pvp";
        public static readonly string SetTeam = "_team";
        public static readonly string TriggerGarbageCollection = "_gc";
        public static readonly string DebugStat = "_debugstat";
        public static readonly string MaxPlayers = "maxplayers";
        public static readonly string TileProvider = "tileprovider";
        public static readonly string Timeout = "settimeout";
        public static readonly string Interval = "setinterval";
        public static readonly string ClearInterval = "clearinterval";
        public static readonly string ShowTimeout = "showdelay";
        public static readonly string RawBroadcast = "rbc";
        public static readonly string Sudo = "runas";
        public static readonly string ListClients = "listclients";
        public static readonly string DumpBuffer = "dumpbuffer";
        public static readonly string TerminateSocket = "kc";
        public static readonly string ResetCharacter = "resetcharacter";
        public static readonly string Ping = "_ping";
        public static readonly string Chat = "_chat";
        public static readonly string ExportCharacter = "exportcharacter";
    }
    public static class DataKey
    {
        public static readonly string IsPE = "chireiden.data.ispe";
    }
    public static readonly string ConfigFile = "chireiden.omni.json";
    public static readonly string VanillaGroup = "chireiden_vanilla";
}