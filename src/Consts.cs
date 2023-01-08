using TerrariaApi.Server;

namespace Chireiden.TShock.Omni;

public partial class Plugin : TerrariaPlugin
{
    public static class Consts
    {
        public static class Permissions
        {
            public readonly static string Whynot = "chireiden.omni.whynot";
            public readonly static string TogglePvP = "chireiden.omni.togglepvp";
            public readonly static string ToggleTeam = "chireiden.omni.toggleteam";
            public readonly static string PvPCommand = "chireiden.omni.setpvp";
            public readonly static string TeamCommand = "chireiden.omni.setteam";
            public readonly static string SyncLoadout = "chireiden.omni.syncloadout";
            public static class Admin
            {
                public readonly static string Ghost = "chireiden.omni.ghost";
                public readonly static string SetLanguage = "chireiden.omni.setlang";
                public readonly static string SetPvp = "chireiden.omni.admin.setpvp";
                public readonly static string SetTeam = "chireiden.omni.admin.setteam";
                public readonly static string TriggerGarbageCollection = "chireiden.omni.admin.gc";
                public readonly static string DebugStat = "chireiden.omni.admin.debugstat";
                public readonly static string MaxPlayers = "chireiden.omni.admin.maxplayers";
                public readonly static string TileProvider = "chireiden.omni.admin.tileprovider";
            }
        }
        public static class Commands
        {
            public readonly static string Whynot = "whynot";
            public readonly static string Ghost = "ghost";
            public readonly static string SetLanguage = "setlang";
            public readonly static string SetPvp = "_pvp";
            public readonly static string SetTeam = "_team";
            public readonly static string TriggerGarbageCollection = "_gc";
            public readonly static string DebugStat = "_debugstat";
            public readonly static string MaxPlayers = "maxplayers";
            public readonly static string TileProvider = "tileprovider";
        }
        public static class DataKey
        {
            public readonly static string Ghost = "chireiden.data.ghost";
            public readonly static string PermissionHistory = "chireiden.data.permissionhistory";
            public readonly static string DetectPE = "chireiden.data.ped";
            public readonly static string IsPE = "chireiden.data.ispe";
            public readonly static string DelayCommands = "chireiden.data.delaycommands";
        }
        public readonly static string ConfigFile = "chireiden.omni.json";
        public readonly static string VanillaGroup = "chireiden_vanilla";
    }
}
