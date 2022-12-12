using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace Chireiden.TShock.Omni;

public partial class Plugin : TerrariaPlugin
{
    private List<Command> _hiddenCommands = new();

    private void PlayerCommand(PlayerCommandEventArgs e)
    {
        var hc = _hiddenCommands.Except(Commands.ChatCommands).ToList().FindAll(c => c.HasAlias(e.CommandName));
        if (hc.Count > 0)
        {
            var ecl = (List<Command>) e.CommandList;
            ecl.AddRange(hc);
        }
        var hidden = Commands.ChatCommands.FindAll(c => this.config.HideCommands.Any(h => c.HasAlias(h)));
        if (hidden.Count > 0)
        {
            Commands.ChatCommands = Commands.ChatCommands.Except(hidden).ToList();
            _hiddenCommands = _hiddenCommands.Concat(hidden).ToList();
        }
    }
}
