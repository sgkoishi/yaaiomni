﻿using System.Threading.Channels;
using TerrariaApi.Server;
using TShockAPI;

namespace Chireiden.TShock.Omni;

public partial class Plugin : TerrariaPlugin
{
    public async Task<TimeSpan> Ping(TSPlayer player)
    {
        return await this.Ping(player, new CancellationTokenSource(1000).Token);
    }

    public async Task<TimeSpan> Ping(TSPlayer player, CancellationToken token)
    {
        var result = TimeSpan.MaxValue;

        var inv = -1;
        for (var i = 0; i < Terraria.Main.item.Length; i++)
        {
            if (!Terraria.Main.item[i].active || Terraria.Main.item[i].playerIndexTheItemIsReservedFor == 255)
            {
                inv = i;
                break;
            }
        }
        if (inv == -1)
        {
            return result;
        }

        var start = DateTime.Now;
        var channel = player.GetOrCreatePlayerAttachedData(Consts.DataKey.PingChannel, this.CreatePingChannel);
        Terraria.NetMessage.TrySendData((int) PacketTypes.RemoveItemOwner, player.Index, -1, null, inv);
        while (!token.IsCancellationRequested)
        {
            var end = await channel.Reader.ReadAsync(token);
            if (end == inv)
            {
                result = DateTime.Now - start;
                break;
            }
        }
        player.SetPlayerAttachedData<Channel<int>?>(Consts.DataKey.PingChannel, null);
        return result;
    }

    private void Hook_Ping_GetData(object? sender, OTAPI.Hooks.MessageBuffer.GetDataEventArgs args)
    {
        if (args.PacketId != (byte) PacketTypes.ItemOwner)
        {
            return;
        }

        var owner = args.Instance.readBuffer[args.ReadOffset + 2];
        if (owner != byte.MaxValue)
        {
            return;
        }

        var whoami = args.Instance.whoAmI;
        var pingresponse = TShockAPI.TShock.Players[whoami]?.GetOrCreatePlayerAttachedData<Channel<int>>(Consts.DataKey.PingChannel, this.CreatePingChannel);
        if (pingresponse == null)
        {
            return;
        }

        var index = BitConverter.ToInt16(args.Instance.readBuffer.AsSpan(args.ReadOffset, 2));
        pingresponse.Writer.TryWrite(index);
    }

    private Channel<int> CreatePingChannel()
    {
        return Channel.CreateBounded<int>(new BoundedChannelOptions(30)
        {
            SingleReader = true,
            SingleWriter = true
        });
    }

    private async void Command_Ping(CommandArgs args)
    {
        try
        {
            var player = args.Player;
            var result = await this.Ping(player);
            player.SendSuccessMessage($"Ping: {result.TotalMilliseconds:F1}ms");
        }
        catch (Exception e)
        {
            args.Player.SendErrorMessage("Ping failed.");
            TShockAPI.TShock.Log.Error(e.ToString());
        }
    }
}
