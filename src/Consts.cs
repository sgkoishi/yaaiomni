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
        public const string ConfigFile = "chireiden.omni.json";
    }
}
