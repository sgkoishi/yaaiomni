using TShockAPI;

namespace Chireiden.TShock.Omni;

public partial class Plugin
{
    private bool Detour_PlayerActive(Func<TSPlayer, bool> orig, TSPlayer player)
    {
        if (player?.TPlayer == null)
        {
            return false;
        }
        var state = this[player].Ghost;
        return state == null ? orig(player) : !state.Value;
    }

    private void OTHook_Ghost_SendBytes(object? sender, OTAPI.Hooks.NetMessage.SendBytesEventArgs args)
    {
        if (args.Data[2] != (int) PacketTypes.PlayerActive)
        {
            return;
        }

        var playerIndex = args.Data[3];
        if (args.RemoteClient != playerIndex)
        {
            var state = this[playerIndex].Ghost;
            if (state == null)
            {
                return;
            }
            args.Data[4] = (byte) (!state).GetHashCode();
        }
    }

    [Command("Admin.Ghost", "ghost", Permission = "chireiden.omni.ghost", AllowServer = false )]
    private void Command_Ghost(CommandArgs args)
    {
        if (args.Parameters.Contains("-v"))
        {
            args.TPlayer.ghost = !args.TPlayer.ghost;
        }
        else if (args.Parameters.Contains("-a"))
        {
            args.TPlayer.active = !args.TPlayer.active;
        }
        else if (args.Parameters.Contains("-u"))
        {
            args.TPlayer.active = true;
            args.TPlayer.ghost = false;
            this[args.Player].Ghost = null;
        }
        else
        {
            var state = this[args.Player].Ghost ?? false;
            this[args.Player].Ghost = !state;
        }
        Terraria.NetMessage.TrySendData((int) PacketTypes.PlayerInfo, -1, args.Player.Index, null, args.Player.Index);
        Terraria.NetMessage.TrySendData((int) PacketTypes.PlayerUpdate, -1, args.Player.Index, null, args.Player.Index);
        Terraria.NetMessage.TrySendData((int) PacketTypes.PlayerActive, -1, args.Player.Index, null, args.Player.Index, args.TPlayer.active.GetHashCode());
    }
}