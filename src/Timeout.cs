using TerrariaApi.Server;
using TShockAPI;

namespace Chireiden.TShock.Omni;

public partial class Plugin : TerrariaPlugin
{
    private int _updateCounter = 0;

    private void Hook_TimeoutInterval(EventArgs args)
    {
        this._updateCounter++;
        foreach (var player in Utils.ActivePlayers)
        {
            this.ProcessDelayCommand(this[player]);
        }

        this.ProcessDelayCommand(this[TSPlayer.Server]);
    }

    private void ProcessDelayCommand(AttachedData data)
    {
        for (var i = 0; i < data.DelayCommands.Count; i++)
        {
            if ((this._updateCounter - data.DelayCommands[i].Start) % data.DelayCommands[i].Timeout == 0)
            {
                TShockAPI.Commands.HandleCommand(data.Player, data.DelayCommands[i].Command);
                data.DelayCommands[i].Repeat -= 1;
            }
            if (data.DelayCommands[i].Repeat == 0)
            {
                data.DelayCommands.RemoveAt(i);
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
        if (!int.TryParse(args.Parameters[1], out var timeout))
        {
            args.Player.SendErrorMessage("Invalid timeout!");
            return;
        }
        if (timeout < 1)
        {
            args.Player.SendErrorMessage("Timeout must be greater than 0!");
            return;
        }

        var commands = this[args.Player].DelayCommands;
        var cmd = new AttachedData.DelayCommand(args.Parameters[0], start: this._updateCounter, timeout: timeout);
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
        if (!int.TryParse(args.Parameters[1], out var interval))
        {
            args.Player.SendErrorMessage("Invalid interval!");
            return;
        }
        if (interval < 1)
        {
            args.Player.SendErrorMessage("Interval must be greater than 0!");
            return;
        }
        var commands = this[args.Player].DelayCommands;
        var cmd = new AttachedData.DelayCommand(args.Parameters[0], start: this._updateCounter, timeout: interval, repeat: 0);
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
        var commands = this[args.Player].DelayCommands;
        if (commands.Count == 0)
        {
            args.Player.SendErrorMessage("No commands found!");
            return;
        }
        for (var i = 0; i < commands.Count; i++)
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
        var commands = this[args.Player].DelayCommands;
        if (commands.Count == 0)
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
