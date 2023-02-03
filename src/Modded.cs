using TerrariaApi.Server;

namespace Chireiden.TShock.Omni;

public partial class Plugin : TerrariaPlugin
{
    private void OTHook_Modded_GetData(object? sender, OTAPI.Hooks.MessageBuffer.GetDataEventArgs args)
    {
        if (args.Result == OTAPI.HookResult.Cancel)
        {
            return;
        }

        var whoAmI = args.Instance.whoAmI;
        var state = Terraria.Netplay.Clients[whoAmI].State;
        var flag = false;
        if (state == -1)
        {
            if (args.PacketId != (byte) PacketTypes.PasswordSend)
            {
                flag = true;
            }
        }
        else if (state == 0)
        {
            if (args.PacketId != (byte) PacketTypes.ConnectRequest)
            {
                flag = true;
            }
        }
        else if (state < 10)
        {
            if (args.PacketId > (byte) PacketTypes.PlayerSpawn
                && args.PacketId != (byte) PacketTypes.SocialHandshake
                && args.PacketId != (byte) PacketTypes.PlayerHp
                && args.PacketId != (byte) PacketTypes.PlayerMana
                && args.PacketId != (byte) PacketTypes.PlayerBuff
                && args.PacketId != (byte) PacketTypes.PasswordSend
                && args.PacketId != (byte) PacketTypes.ClientUUID
                && args.PacketId != (byte) PacketTypes.SyncLoadout)
            {
                flag = true;
            }
        }

        if (flag)
        {
            Terraria.NetMessage.TrySendData((int) PacketTypes.Disconnect, whoAmI, -1, Terraria.Lang.mp[1].ToNetworkText());
            // Stop handling any data
            Terraria.Netplay.Clients[whoAmI].PendingTermination = true;
            Terraria.Netplay.Clients[whoAmI].PendingTerminationApproved = true;
            args.Result = OTAPI.HookResult.Cancel;
            // FIXME: TSAPI is not respecting args.Result, so we have to craft invalid packet.
            args.PacketId = byte.MaxValue;
        }
    }
}
