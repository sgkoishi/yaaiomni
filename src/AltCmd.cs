using TerrariaApi.Server;

namespace Chireiden.TShock.Omni;

public partial class Plugin : TerrariaPlugin
{
    public class ParsedCommand
    {
        public string Command { get; set; } = string.Empty;
        public List<string> Parameters { get; set; } = new List<string>();
        internal bool ContinueOnError = false;
        private string _buffer = string.Empty;
        internal bool IsEmpty => string.IsNullOrEmpty(this.Command);
        internal void AppendChar(char c)
        {
            this._buffer += c;
        }
        internal void EndSegment()
        {
            if (string.IsNullOrEmpty(this.Command) && !string.IsNullOrWhiteSpace(this._buffer))
            {
                this.Command = this._buffer.Trim();
            }
            else
            {
                this.Parameters.Add(this._buffer);
            }
            this._buffer = string.Empty;
        }
    }
    private bool Detour_Command_Alternative(Func<TShockAPI.TSPlayer, string, bool> orig, TShockAPI.TSPlayer player, string text)
    {
        if (this.config.Enhancements.AlternativeCommandSyntax)
        {
            var commands = Utils.ParseCommands(text);
            foreach (var command in commands)
            {
                var cmd = Utils.ToCommand(command.Command, command.Parameters);
                if (!cmd.StartsWith(TShockAPI.Commands.Specifier) && !cmd.StartsWith(TShockAPI.Commands.SilentSpecifier))
                {
                    cmd = TShockAPI.Commands.Specifier + cmd;
                }
                try
                {
                    orig(player, cmd);
                }
                catch when (command.ContinueOnError)
                {
                }
            }
            return true;
        }
        else
        {
            return orig(player, text);
        }
    }
}
