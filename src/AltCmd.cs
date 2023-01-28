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
                var cmdName = command[0];
                if (!cmdName.StartsWith(TShockAPI.Commands.Specifier) && !cmdName.StartsWith(TShockAPI.Commands.SilentSpecifier))
                {
                    cmdName = TShockAPI.Commands.Specifier + cmdName;
                }
                orig(player, Utils.ToCommand(cmdName, command.GetRange(1, command.Count - 1)));
            }
            return true;
        }
        else
        {
            return orig(player, text);
        }
    }
}
