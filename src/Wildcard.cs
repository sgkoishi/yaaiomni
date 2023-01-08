using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace Chireiden.TShock.Omni;

public partial class Plugin : TerrariaPlugin
{
    private void Hook_Wildcard_PlayerCommand(PlayerCommandEventArgs e)
    {
        for (var i = 0; i < e.Parameters.Count; i++)
        {
            var arg = e.Parameters[i];
            if (this.config.PlayerWildcardFormat.Contains(arg))
            {
                e.Handled = true;
                foreach (var player in TShockAPI.TShock.Players)
                {
                    if (player is null || !player.Active)
                    {
                        continue;
                    }

                    var newargs = e.Parameters.ToList();
                    newargs[i] = player.Name;
                    TShockAPI.Commands.HandleCommand(player, Utils.ToCommand(e.CommandPrefix, e.CommandName, newargs));
                }
                return;
            }
        }
    }
}
