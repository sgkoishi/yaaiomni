using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using TShockAPI;

namespace Chireiden.TShock.Omni;

public partial class Plugin
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
        if (this.config.Enhancements.Value.AlternativeCommandSyntax)
        {
            var commands = Utils.ParseCommands(text);
            foreach (var command in commands)
            {
                var cmd = Utils.ToCommand(command.Command, command.Parameters);
                if (!cmd.StartsWith(TShockAPI.Commands.Specifier) && !cmd.StartsWith(TShockAPI.Commands.SilentSpecifier))
                {
                    cmd = TShockAPI.Commands.Specifier + cmd;
                }
                if (command.ContinueOnError)
                {
                    this.HandleCommandCatched(orig, player, cmd);
                }
                else
                {
                    this.HandleCommandUncatched(orig, player, cmd);
                }
            }
            return true;
        }
        else
        {
            return this.HandleCommandCatched(orig, player, text);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    private bool HandleCommandUncatched(Func<TShockAPI.TSPlayer, string, bool> orig, TShockAPI.TSPlayer player, string text)
    {
        return orig(player, text);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    private bool HandleCommandCatched(Func<TShockAPI.TSPlayer, string, bool> orig, TShockAPI.TSPlayer player, string text)
    {
        return orig(player, text);
    }

    private static readonly MethodInfo _uncatchedHandleCommand = Utils.Method(() =>
            new Plugin(null!).HandleCommandUncatched(null!, null!, null!))!;
    private static readonly MethodInfo _catchedHandleCommand = Utils.Method(() =>
            new Plugin(null!).HandleCommandCatched(null!, null!, null!))!;

    private bool Detour_Command_Run(Func<Command, string, bool, TSPlayer, List<string>, bool> orig, Command instance, string msg, bool silent, TSPlayer ply, List<string> parms)
    {
        var trace = new StackTrace();
        foreach (var frame in trace.GetFrames())
        {
            if (frame.GetMethod() == _catchedHandleCommand)
            {
                return orig(instance, msg, silent, ply, parms);
            }
            else
            {
                if (!instance.CanRun(ply))
                {
                    return false;
                }
                instance.CommandDelegate(new CommandArgs(msg, silent, ply, parms));
                return true;
            }
        }

        return orig(instance, msg, silent, ply, parms);
    }
}