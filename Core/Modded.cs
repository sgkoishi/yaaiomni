namespace Chireiden.TShock.Omni;

public partial class Plugin
{
    private void OTHook_Modded_GetData(object? sender, OTAPI.Hooks.MessageBuffer.GetDataEventArgs args)
    {
        static bool ModdedEarlyChatSpam(int whoAmI, byte packetId)
        {
            var state = Terraria.Netplay.Clients[whoAmI].State;
            if (state == -1)
            {
                if (packetId != (byte) PacketTypes.PasswordSend)
                {
                    return true;
                }
            }
            else if (state == 0)
            {
                if (packetId != (byte) PacketTypes.ConnectRequest)
                {
                    return true;
                }
            }
            else if (state < 10)
            {
                if (packetId > (byte) PacketTypes.PlayerSpawn
                    && packetId != (byte) PacketTypes.SocialHandshake
                    && packetId != (byte) PacketTypes.PlayerHp
                    && packetId != (byte) PacketTypes.PlayerMana
                    && packetId != (byte) PacketTypes.PlayerBuff
                    && packetId != (byte) PacketTypes.PasswordSend
                    && packetId != (byte) PacketTypes.ClientUUID
                    && packetId != (byte) PacketTypes.SyncLoadout)
                {
                    return true;
                }
            }
            return false;
        }

        static bool ModdedFakeName(int whoAmI, Span<byte> data)
        {
            if (Terraria.Netplay.Clients[whoAmI].State < 10)
            {
                return false;
            }
            var currentName = Terraria.Main.player[whoAmI].name;
            using var ms = new MemoryStream(data.ToArray());
            using var br = new BinaryReader(ms);
            var newName = br.ReadString();
            if (newName != currentName)
            {
                TShockAPI.TShock.Log.ConsoleInfo($"Unusual name change detected: {Terraria.Netplay.Clients[whoAmI].Socket.GetRemoteAddress()} claimed the name \"{newName}\" but previously known as {currentName}");
                return true;
            }
            return false;
        }

        if (args.Result == OTAPI.HookResult.Cancel)
        {
            return;
        }

        var whoAmI = args.Instance.whoAmI;
        if (ModdedEarlyChatSpam(whoAmI, args.PacketId))
        {
            Terraria.NetMessage.TrySendData((int) PacketTypes.Disconnect, whoAmI, -1, Terraria.Lang.mp[1].ToNetworkText());
            this.Statistics.ModdedEarlyChatSpam++;
            TShockAPI.TShock.Log.ConsoleInfo($"Unusual chat detected and disconnected. ({Terraria.Netplay.Clients[whoAmI].Socket.GetRemoteAddress()})");
            // Stop handling any data
            Terraria.Netplay.Clients[whoAmI].PendingTermination = true;
            Terraria.Netplay.Clients[whoAmI].PendingTerminationApproved = true;
            args.CancelPacket();
        }

        if (args.PacketId == (byte) PacketTypes.PlayerInfo)
        {
            // This is actually not working since the client do not sync
            // Only sent when related info changed
            if (ModdedFakeName(whoAmI, args.Instance.readBuffer.AsSpan(args.ReadOffset + 3, args.Length - 3)))
            {
                Terraria.NetMessage.TrySendData((int) PacketTypes.Disconnect, whoAmI, -1, Terraria.Lang.mp[1].ToNetworkText());
                this.Statistics.ModdedFakeName++;
                Terraria.Netplay.Clients[whoAmI].PendingTermination = true;
                Terraria.Netplay.Clients[whoAmI].PendingTerminationApproved = true;
                args.CancelPacket();
            }

            // It might be possible to detect via active probing:
            // if (player.position.Y < Main.worldSurface * 16.0)
            // {
            //     player.happyFunTorchTime = true;
            //     SendData(PacketTypes.PlayerInfo);
            //     if (player.name != GetData(PacketTypes.PlayerInfo).name)
            //     {
            //         Kick(player);
            //     }
            // }
        }
    }
}