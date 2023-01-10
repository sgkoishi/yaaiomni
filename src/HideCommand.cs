using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace Chireiden.TShock.Omni;

public partial class Plugin : TerrariaPlugin
{
    private List<Command> _hiddenCommands = new();

    private void Hook_HideCommand_PlayerCommand(PlayerCommandEventArgs args)
    {
        var hc = this._hiddenCommands.FindAll(c => c.HasAlias(args.CommandName) && c.CanRun(args.Player));
        if (hc.Count > 0)
        {
            var ecl = (List<Command>) args.CommandList;
            ecl.AddRange(hc.Except(Commands.ChatCommands));
        }
        var hidden = Commands.ChatCommands.FindAll(c => this.config.HideCommands.Any(h => c.HasAlias(h)));
        if (hidden.Count > 0)
        {
            Commands.ChatCommands = Commands.ChatCommands.Except(hidden).ToList();
            this._hiddenCommands = this._hiddenCommands.Concat(hidden).ToList();
        }
    }
}
