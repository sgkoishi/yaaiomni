using TerrariaApi.Server;
using TShockAPI;

namespace Chireiden.TShock.Omni;

public partial class Plugin : TerrariaPlugin
{
    private int _updateCounter = 0;
    public class DelayCommand
    {
        public string Command { get; set; }
        public TSPlayer Player { get; set; }
        public int Timeout { get; set; }
        internal int Start { get; set; }
        public int Repeat { get; set; }
        public DelayCommand(string command, TSPlayer player, int start = 0, int timeout = 60, int repeat = 1)
        {
            this.Command = command;
            this.Player = player;
            this.Timeout = timeout;
            this.Start = this.Timeout;
            this.Repeat = repeat;
        }
    }

    private void Hook_TimeoutInterval(EventArgs args)
    {
        this._updateCounter++;
        foreach (var player in TShockAPI.TShock.Players)
        {
            if (player.GetData<List<DelayCommand>>(Consts.DataKey.DelayCommands) is not List<DelayCommand> commands)
            {
                continue;
            }
            this.ProcessDelayCommand(player, commands);
        }

        if (TSPlayer.Server.GetData<List<DelayCommand>>(Consts.DataKey.DelayCommands) is not List<DelayCommand> serverCommands)
        {
            return;
        }
        this.ProcessDelayCommand(TSPlayer.Server, serverCommands);
    }

    private void ProcessDelayCommand(TSPlayer player, List<DelayCommand> command)
    {
        for (int i = 0; i < command.Count; i++)
        {
            command[i].Repeat -= 1;
            if ((this._updateCounter - command[i].Start) % command[i].Timeout == 0)
            {
                TShockAPI.Commands.HandleCommand(player, command[i].Command);
            }
            if (command[i].Repeat == 0)
            {
                command.RemoveAt(i);
                i--;
            }
        }
    }

    private void Command_SetTimeout(CommandArgs args)
    {
        if (args.Parameters.Count < 2)
        {
            args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /settimeout <command> <timeout>");
            return;
        }
        if (!int.TryParse(args.Parameters[1], out int timeout))
        {
            args.Player.SendErrorMessage("Invalid timeout!");
            return;
        }
        if (timeout < 1)
        {
            args.Player.SendErrorMessage("Timeout must be greater than 0!");
            return;
        }
        if (args.Parameters[0].StartsWith("/"))
        {
            args.Parameters[0] = args.Parameters[0].Substring(1);
        }
        if (args.Player.GetData<List<DelayCommand>>(Consts.DataKey.DelayCommands) is not List<DelayCommand> commands)
        {
            args.Player.SetData(Consts.DataKey.DelayCommands, commands = new List<DelayCommand>());
        }
        var cmd = new DelayCommand(args.Parameters[0], args.Player, start: this._updateCounter, timeout: timeout);
        commands.Add(cmd);
        args.Player.SendSuccessMessage($"Command {args.Parameters[0]} will be executed once in the future (id: {(uint) cmd.GetHashCode()}).");
    }

    private void Command_SetInterval(CommandArgs args)
    {
        if (args.Parameters.Count < 2)
        {
            args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /setinterval <command> <interval>");
            return;
        }
        if (!int.TryParse(args.Parameters[1], out int interval))
        {
            args.Player.SendErrorMessage("Invalid interval!");
            return;
        }
        if (interval < 1)
        {
            args.Player.SendErrorMessage("Interval must be greater than 0!");
            return;
        }
        if (args.Parameters[0].StartsWith("/"))
        {
            args.Parameters[0] = args.Parameters[0].Substring(1);
        }
        if (args.Player.GetData<List<DelayCommand>>(Consts.DataKey.DelayCommands) is not List<DelayCommand> commands)
        {
            args.Player.SetData(Consts.DataKey.DelayCommands, commands = new List<DelayCommand>());
        }
        var cmd = new DelayCommand(args.Parameters[0], args.Player, start: this._updateCounter, timeout: interval, repeat: 0);
        commands.Add(cmd);
        args.Player.SendSuccessMessage($"Command {args.Parameters[0]} will be executed in the future (id: {(uint) cmd.GetHashCode()}).");
    }

    private void Command_ClearInterval(CommandArgs args)
    {
        if (args.Parameters.Count < 1)
        {
            args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /clearinterval <id>");
            return;
        }
        if (!uint.TryParse(args.Parameters[0], out var id))
        {
            args.Player.SendErrorMessage("Invalid id!");
            return;
        }
        if (args.Player.GetData<List<DelayCommand>>(Consts.DataKey.DelayCommands) is not List<DelayCommand> commands)
        {
            args.Player.SendErrorMessage("No commands found!");
            return;
        }
        for (int i = 0; i < commands.Count; i++)
        {
            var cmd = commands[i];
            if ((uint) cmd.GetHashCode() == id)
            {
                commands.RemoveAt(i);
                args.Player.SendSuccessMessage($"Command {id} ({cmd.Command}) has been removed.");
                return;
            }
        }
    }

    private void Command_ListDelay(CommandArgs args)
    {
        if (args.Player.GetData<List<DelayCommand>>(Consts.DataKey.DelayCommands) is not List<DelayCommand> commands)
        {
            args.Player.SendErrorMessage("No commands found!");
            return;
        }
        args.Player.SendInfoMessage("Commands:");
        foreach (var cmd in commands)
        {
            args.Player.SendInfoMessage($"Command: {cmd.Command}, Timeout: {cmd.Timeout}, Repeat: {cmd.Repeat}, Id: {(uint) cmd.GetHashCode()}");
        }
    }
}
