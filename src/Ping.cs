using System.Threading.Channels;
using TerrariaApi.Server;
using TShockAPI;

namespace Chireiden.TShock.Omni;

public partial class Plugin : TerrariaPlugin
{
    public TimeSpan? TryPing(TSPlayer player)
    {
        _ = this.Ping(player, new CancellationTokenSource(1000).Token);
        return this[player].PingChannel.LastPing;
    }

    public async Task<TimeSpan> Ping(TSPlayer player)
    {
        return await this.Ping(player, new CancellationTokenSource(1000).Token);
    }

    public async Task<TimeSpan> Ping(TSPlayer player, CancellationToken token)
    {
        var pingdata = this[player].PingChannel;
        var inv = -1;
        for (var i = 0; i < Terraria.Main.item.Length; i++)
        {
            if (!Terraria.Main.item[i].active || Terraria.Main.item[i].playerIndexTheItemIsReservedFor == 255)
            {
                if (pingdata.RecentPings[inv]?.Channel == null)
                {
                    inv = i;
                    break;
                }
            }
        }
        if (inv == -1)
        {
            return TimeSpan.MaxValue;
        }
        var pd = pingdata.RecentPings[inv] ??= new AttachedData.PingDetails();
        pd.Channel ??= Channel.CreateBounded<int>(new BoundedChannelOptions(30)
        {
            SingleReader = true,
            SingleWriter = true
        });
        Terraria.NetMessage.TrySendData((int) PacketTypes.RemoveItemOwner, player.Index, -1, null, inv);
        await pd.Channel.Reader.ReadAsync(token);
        pd.Channel = null;
        return (pingdata.LastPing = pd.End!.Value - pd.Start).Value;
    }

    private void OTHook_Ping_GetData(object? sender, OTAPI.Hooks.MessageBuffer.GetDataEventArgs args)
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
        var pingresponse = this[whoami].PingChannel;
        var index = BitConverter.ToInt16(args.Instance.readBuffer.AsSpan(args.ReadOffset, 2));
        var ping = pingresponse?.RecentPings[index];
        if (ping != null)
        {
            ping.End = DateTime.Now;
            ping.Channel!.Writer.TryWrite(index);
        }
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
