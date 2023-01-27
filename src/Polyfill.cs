using TerrariaApi.Server;

namespace Chireiden.TShock.Omni;

public partial class Plugin : TerrariaPlugin
{
    private bool Detour_Backport_2894(Func<TShockAPI.DB.CharacterManager, TShockAPI.TSPlayer, bool, bool> orig,
        TShockAPI.DB.CharacterManager self, TShockAPI.TSPlayer player, bool fromCommand)
    {
        // FIXME: This is a backport of Pryaxis/TShock#2894
        return player.State >= 10 && orig(self, player, fromCommand);
    }
}
