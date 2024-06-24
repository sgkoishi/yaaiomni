namespace Chireiden.TShock.Omni;

public partial class Plugin
{
    private readonly HashSet<PacketTypes> AllowedPackets = new HashSet<PacketTypes>();
    private void OTHook_Modded_GetData(object? sender, OTAPI.Hooks.MessageBuffer.GetDataEventArgs args)
    {
        static bool ModdedEarlyChatSpam(int whoAmI, PacketTypes packetId, HashSet<PacketTypes> allowedPackets)
        {
            var state = Terraria.Netplay.Clients[whoAmI].State;
            if (state == -1)
            {
                return packetId is not PacketTypes.PasswordSend;
            }
            else if (state == 0)
            {
                return packetId is not PacketTypes.ConnectRequest;
            }
            else if (state < 10)
            {
                if (packetId > PacketTypes.PlayerSpawn
                    && packetId is not (PacketTypes.SocialHandshake
                        or PacketTypes.PlayerHp or PacketTypes.PlayerMana or PacketTypes.PlayerBuff
                        or PacketTypes.PasswordSend or PacketTypes.ClientUUID or PacketTypes.SyncLoadout)
                    // Cross-platform client
                    && packetId != (PacketTypes) 150
                    // Cancelled packets from earlier hooks
                    && packetId != (PacketTypes) 255)
                {
                    if (TShockAPI.TShock.Players[whoAmI].IgnoreSSCPackets && packetId is PacketTypes.ItemOwner)
                    {
                        // https://github.com/Pryaxis/TShock/commit/fd5c696656ecdfc8346ed67146baaa04589e01e4
                        // TShock use RemoveItemOwner(400) to ping the client after SSC
                        return false;
                    }
                    if (Terraria.Main.ServerSideCharacter && packetId is PacketTypes.LoadNetModule)
                    {
                        // TShock send SyncLoadout and the client returns a LoadNetModule(LoadoutChange)
                        return false;
                    }
                    if (allowedPackets.Contains(packetId))
                    {
                        // Dimensions use Placeholder 67 to show the IP address
                        return false;
                    }
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
        if (ModdedEarlyChatSpam(whoAmI, (PacketTypes) args.PacketId, this.AllowedPackets))
        {
            this.Statistics.ModdedEarlyChatSpam++;
            TShockAPI.TShock.Log.ConsoleInfo($"Unusual packet {args.PacketId} detected at state {Terraria.Netplay.Clients[whoAmI].State} and disconnected. ({Terraria.Netplay.Clients[whoAmI].Socket.GetRemoteAddress()})");
            args.CancelPacket();
            Terraria.Netplay.Clients[whoAmI].PendingTermination = true;
            Terraria.Netplay.Clients[whoAmI].PendingTerminationApproved = true;
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