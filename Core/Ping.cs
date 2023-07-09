using TShockAPI;

namespace Chireiden.TShock.Omni;

public partial class Plugin
{
    public void Ping(TSPlayer player, Action<TSPlayer, TimeSpan> callback)
    {
        var pingdata = this[player].RecentPings;
        var items = Terraria.Main.item
            .Select((item, index) => (item, index))
            .Where(value => !value.item.active || value.item.playerIndexTheItemIsReservedFor == 255)
            .Select(value => value.index)
            .ToArray();

        if (items.Length == 0)
        {
            items = Terraria.Main.item.Select((_item, index) => index).ToArray();
        }

        var preferred = items.Where(i => pingdata[i]?.Start == null).ToArray();
        if (preferred.Length == 0)
        {
            preferred = items.OrderBy(i => pingdata[i].Start!.Value).ToArray();
        }

        var index = items[0];
        pingdata[index].Start = DateTime.Now;
        pingdata[index].Callback = callback;
        Terraria.NetMessage.TrySendData((int) PacketTypes.RemoveItemOwner, player.Index, -1, null, index);
    }

    private void OTHook_Ping_SendBytes(object? sender, OTAPI.Hooks.NetMessage.SendBytesEventArgs args)
    {
        if (args.Data[2] != (int) PacketTypes.RemoveItemOwner)
        {
            return;
        }

        var whoami = args.RemoteClient;
        var index = BitConverter.ToInt16(args.Data.AsSpan(3, 2));
        var ping = this[whoami].RecentPings[index];
        ping.Start = DateTime.Now;
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

        var whoami = TShockAPI.TShock.Players[args.Instance.whoAmI];
        var ping = this[whoami].RecentPings[BitConverter.ToInt16(args.Instance.readBuffer.AsSpan(args.ReadOffset, 2))];

        if (ping.Start.HasValue)
        {
            ping.End = DateTime.Now;
            try
            {
                this[whoami].OnPingUpdated?.Invoke(DateTime.Now - ping.Start.Value);
            }
            catch
            {
            }
            try
            {
                ping.Callback?.Invoke(whoami, DateTime.Now - ping.Start.Value);
            }
            catch
            {
            }
            finally
            {
                ping.Callback = null;
            }
        }
    }

    [Command("Ping", "_ping", AllowServer = false, Permission = "chireiden.omni.ping")]
    private void Command_Ping(CommandArgs args)
    {
        try
        {
            var player = args.Player;
            this.Ping(player, (p, t) => p?.SendSuccessMessage($"Ping: {t.TotalMilliseconds:F1}ms"));
        }
        catch (Exception e)
        {
            args.Player.SendErrorMessage("Ping failed.");
            TShockAPI.TShock.Log.Error(e.ToString());
        }
    }
}