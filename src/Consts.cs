using TerrariaApi.Server;

namespace Chireiden.TShock.Omni;

public partial class Plugin : TerrariaPlugin
{
    public static class Consts
    {
        public static class Permissions
        {
            public const string Ghost = "chireiden.omni.ghost";
            public const string Whynot = "chireiden.omni.whynot";
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
