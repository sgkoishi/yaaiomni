using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace Chireiden.TShock.Omni;

public partial class Plugin : TerrariaPlugin
{
    private void Hook_Wildcard_PlayerCommand(PlayerCommandEventArgs args)
    {
        if (args.Handled)
        {
            return;
        }

        for (var i = 0; i < args.Parameters.Count; i++)
        {
            var arg = args.Parameters[i];
            if (this.config.PlayerWildcardFormat.Contains(arg))
            {
                args.Handled = true;
                foreach (var player in Utils.ActivePlayers)
                {
                    var newargs = args.Parameters.ToList();
                    newargs[i] = player.Name;
                    TShockAPI.Commands.HandleCommand(player, Utils.ToCommand(args.CommandPrefix, args.CommandName, newargs));
                }
                return;
            }
        }
    }

    private List<TSPlayer> Hook_Wildcard_GetPlayers(Func<string, List<TSPlayer>> orig, string name)
    {
        return this.config.ServerWildcardFormat.Contains(name) ? new List<TSPlayer> { TSPlayer.Server } : orig(name);
    }
}
