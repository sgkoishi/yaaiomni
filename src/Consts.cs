using TerrariaApi.Server;

namespace Chireiden.TShock.Omni;

public partial class Plugin : TerrariaPlugin
{
    public static class Consts
    {
        public static class Permissions
        {
            public const string Whynot = "chireiden.omni.whynot";
            public const string TogglePvP = "chireiden.omni.togglepvp";
            public const string ToggleTeam = "chireiden.omni.toggleteam";
            public const string PvPCommand = "chireiden.omni.setpvp";
            public const string TeamCommand = "chireiden.omni.setteam";
            public static class Admin
            {
                public const string Ghost = "chireiden.omni.ghost";
                public const string SetLanguage = "chireiden.omni.setlang";
                public const string SetPvp = "chireiden.omni.admin.setpvp";
                public const string SetTeam = "chireiden.omni.admin.setteam";
                public const string TriggerGarbageCollection = "chireiden.omni.admin.gc";
            }
        }
        public static class Commands
        {
            public const string Whynot = "whynot";
            public const string Ghost = "ghost";
            public const string SetLanguage = "setlang";
            public const string SetPvp = "_pvp";
            public const string SetTeam = "_team";
            public const string TriggerGarbageCollection = "_gc";
        }
        public static class DataKey
        {
            public const string Ghost = "chireiden.data.ghost";
            public const string PermissionHistory = "chireiden.data.permissionhistory";
        }
        public const string ConfigFile = "chireiden.omni.json";
        public const string VanillaGroup = "chireiden_vanilla";
    }
}
