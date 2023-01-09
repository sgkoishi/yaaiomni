using TerrariaApi.Server;

namespace Chireiden.TShock.Omni;

public partial class Plugin : TerrariaPlugin
{
    private void Hook_Modded_GetData(object? sender, OTAPI.Hooks.MessageBuffer.GetDataEventArgs e)
    {
        if (e.Result == OTAPI.HookResult.Cancel)
        {
            return;
        }

        var whoAmI = e.Instance.whoAmI;
        var state = Terraria.Netplay.Clients[whoAmI].State;
        var flag = false;
        if (state == -1)
        {
            if (e.PacketId != (byte) PacketTypes.PasswordSend)
            {
                flag = true;
            }
        }
        else if (state == 0)
        {
            if (e.PacketId != (byte) PacketTypes.ConnectRequest)
            {
                flag = true;
            }
        }
        else if (state < 10)
        {
            if (e.PacketId > (byte) PacketTypes.PlayerSpawn
                && e.PacketId != (byte) PacketTypes.SocialHandshake
                && e.PacketId != (byte) PacketTypes.PlayerHp
                && e.PacketId != (byte) PacketTypes.PlayerMana
                && e.PacketId != (byte) PacketTypes.PlayerBuff
                && e.PacketId != (byte) PacketTypes.PasswordSend
                && e.PacketId != (byte) PacketTypes.ClientUUID
                && e.PacketId != (byte) PacketTypes.SyncLoadout)
            {
                flag = true;
            }
        }

        if (flag)
        {
            Terraria.NetMessage.TrySendData(2, whoAmI, -1, Terraria.Lang.mp[1].ToNetworkText());
            // Stop handling any data
            Terraria.Netplay.Clients[whoAmI].PendingTermination = true;
            Terraria.Netplay.Clients[whoAmI].PendingTerminationApproved = true;
            e.Result = OTAPI.HookResult.Cancel;
        }
    }
}

