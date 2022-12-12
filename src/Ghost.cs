using System.Runtime.CompilerServices;
using TerrariaApi.Server;
using TShockAPI;

namespace Chireiden.TShock.Omni;

public partial class Plugin : TerrariaPlugin
{
    // ConditionalWeakTable<_, TValue> where TValue : class
    // So we need a wrapper class
    public class GhostState
    {
        public bool Ghost;
    }

    private readonly ConditionalWeakTable<TSPlayer, GhostState> _ghost = new();

    private void Ghost_SendBytes(object? sender, OTAPI.Hooks.NetMessage.SendBytesEventArgs e)
    {
        if (e.Data[2] != (int) PacketTypes.PlayerActive)
        {
            return;
        }
        var playerIndex = e.Data[3];
        if (e.RemoteClient != playerIndex)
        {
            lock (this._ghost)
            {
                foreach (var players in _ghost)
                {
                    if (players.Key.Index == playerIndex)
                    {
                        e.Data[4] = (byte) players.Value.Ghost.GetHashCode();
                        break;
                    }
                }
            }
        }
    }

    private void GhostCommand(CommandArgs args)
    {
        lock (this._ghost)
        {
            var state = this._ghost.GetOrCreateValue(args.Player)!;
            state.Ghost = !state.Ghost;
        }
        Terraria.NetMessage.SendData(14, -1, args.Player.Index, null, args.Player.Index, args.TPlayer.active.GetHashCode());
    }
}
