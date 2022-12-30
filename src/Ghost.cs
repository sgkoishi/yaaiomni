﻿using TerrariaApi.Server;
using TShockAPI;

namespace Chireiden.TShock.Omni;

public partial class Plugin : TerrariaPlugin
{
    private bool PlayerActive(Func<TSPlayer, bool> orig, TSPlayer player)
    {
        if (player?.TPlayer == null)
        {
            return false;
        }
        var state = player.GetData<bool?>(Consts.DataKey.Ghost);
        return state == null ? orig(player) : !state.Value;
    }

    private void Ghost_SendBytes(object? sender, OTAPI.Hooks.NetMessage.SendBytesEventArgs e)
    {
        if (e.Data[2] != (int) PacketTypes.PlayerActive)
        {
            return;
        }

        var playerIndex = e.Data[3];
        if (e.RemoteClient != playerIndex)
        {
            var state = TShockAPI.TShock.Players[playerIndex]?.GetData<bool?>(Consts.DataKey.Ghost);
            if (state == null)
            {
                return;
            }
            e.Data[4] = (byte) (!state).GetHashCode();
        }
    }

    private void GhostCommand(CommandArgs args)
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
        Terraria.NetMessage.TrySendData(4, -1, args.Player.Index, null, args.Player.Index);
        Terraria.NetMessage.TrySendData(13, -1, args.Player.Index, null, args.Player.Index);
        Terraria.NetMessage.TrySendData(14, -1, args.Player.Index, null, args.Player.Index, args.TPlayer.active.GetHashCode());
    }
}
