using TerrariaApi.Server;
using TShockAPI;

namespace Chireiden.TShock.Omni;

public partial class Plugin : TerrariaPlugin
{
    private bool Detour_PlayerActive(Func<TSPlayer, bool> orig, TSPlayer player)
    {
        if (player?.TPlayer == null)
        {
            return false;
        }
        var state = player.GetData<bool?>(Consts.DataKey.Ghost);
        return state == null ? orig(player) : !state.Value;
    }

    private void Hook_Ghost_SendBytes(object? sender, OTAPI.Hooks.NetMessage.SendBytesEventArgs args)
    {
        if (args.Data[2] != (int) PacketTypes.PlayerActive)
        {
            return;
        }

        var playerIndex = args.Data[3];
        if (args.RemoteClient != playerIndex)
        {
            var state = TShockAPI.TShock.Players[playerIndex]?.GetData<bool?>(Consts.DataKey.Ghost);
            if (state == null)
            {
                return;
            }
            args.Data[4] = (byte) (!state).GetHashCode();
        }
    }

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
            args.Player.SetData<bool?>(Consts.DataKey.Ghost, null);
        }
        else
        {
            var state = args.Player.GetData<bool?>(Consts.DataKey.Ghost) ?? false;
            args.Player.SetData<bool?>(Consts.DataKey.Ghost, !state);
        }
        Terraria.NetMessage.TrySendData((int) PacketTypes.PlayerInfo, -1, args.Player.Index, null, args.Player.Index);
        Terraria.NetMessage.TrySendData((int) PacketTypes.PlayerUpdate, -1, args.Player.Index, null, args.Player.Index);
        Terraria.NetMessage.TrySendData((int) PacketTypes.PlayerActive, -1, args.Player.Index, null, args.Player.Index, args.TPlayer.active.GetHashCode());
    }
}
