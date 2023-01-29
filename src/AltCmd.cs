using TerrariaApi.Server;

namespace Chireiden.TShock.Omni;

public partial class Plugin : TerrariaPlugin
{
    private bool Detour_Command_Alternative(Func<TShockAPI.TSPlayer, string, bool> orig, TShockAPI.TSPlayer player, string text)
    {
        if (this.config.AlternativeCommandSyntax)
        {
            var commands = Utils.ParseCommands(text);
            foreach (var command in commands)
            {
                if (command.Count == 0)
                {
                    continue;
                }
                var cmd = Utils.ToCommand(command[0], command.GetRange(1, command.Count - 1));
                if (!cmd.StartsWith(TShockAPI.Commands.Specifier) && !cmd.StartsWith(TShockAPI.Commands.SilentSpecifier))
                {
                    cmd = TShockAPI.Commands.Specifier + cmd;
                }
                orig(player, cmd);
            }
            return true;
        }
        else
        {
            return orig(player, text);
        }
    }
}
