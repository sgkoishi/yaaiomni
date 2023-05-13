using System.Net.Sockets;
using Terraria;
using TerrariaApi.Server;

namespace Chireiden.TShock.Omni;

partial class Plugin
{
    private void MMHook_DebugPacket_GetData(On.Terraria.MessageBuffer.orig_GetData orig, MessageBuffer self, int start, int length, out int messageType)
    {
        if (this.config.DebugPacket.In)
        {
            TShockAPI.TShock.Log.ConsoleInfo($"[DbgPkt] I ->{self.whoAmI} [{length:X4}] {(PacketTypes) self.readBuffer[start]} {BitConverter.ToString(self.readBuffer, start + 1, length - 1)}");
        }
        orig(self, start, length, out messageType);
    }

    private void MMHook_DebugPacket_SendData(On.Terraria.NetMessage.orig_SendData orig, int msgType, int remoteClient, int ignoreClient, Terraria.Localization.NetworkText text, int number, float number2, float number3, float number4, int number5, int number6, int number7)
    {
        if (this.config.DebugPacket.Out)
        {
            TShockAPI.TShock.Log.ConsoleInfo($"[DbgPkt] O ->{remoteClient} except {ignoreClient} {(PacketTypes) msgType} {text} ({number}, {number2}, {number3}, {number4}, {number5}, {number6}, {number7})");
        }
        orig(msgType, remoteClient, ignoreClient, text, number, number2, number3, number4, number5, number6, number7);
    }

    private void OTHook_DebugPacket_SendBytes(object? sender, OTAPI.Hooks.NetMessage.SendBytesEventArgs args)
    {
        if (this.config.DebugPacket.BytesOut)
        {
            TShockAPI.TShock.Log.ConsoleInfo($"[DbgPkt] O ->{args.RemoteClient} {(PacketTypes) args.Data[args.Offset + 2]} {args.Result != OTAPI.HookResult.Cancel} {BitConverter.ToString(args.Data, args.Offset, args.Size)}");
        }
    }

    private void MMHook_DebugPacket_CatchGet(On.Terraria.MessageBuffer.orig_GetData orig, MessageBuffer self, int start, int length, out int messageType)
    {
        try
        {
            orig(self, start, length, out messageType);
        }
        catch (SocketException se)
        {
            if (this.config.DebugPacket.ShowCatchedException > Config.DebugPacketSettings.CatchedException.Uncommon)
            {
                TShockAPI.TShock.Log.ConsoleError($"{se}");
            }
            throw;
        }
        catch (Exception ex)
        {
            if (this.config.DebugPacket.ShowCatchedException > Config.DebugPacketSettings.CatchedException.None)
            {
                TShockAPI.TShock.Log.ConsoleError($"{ex}");
            }
            throw;
        }
    }

    private void MMHook_DebugPacket_CatchSend(On.Terraria.NetMessage.orig_SendData orig, int msgType, int remoteClient, int ignoreClient, Terraria.Localization.NetworkText text, int number, float number2, float number3, float number4, int number5, int number6, int number7)
    {
        try
        {
            orig(msgType, remoteClient, ignoreClient, text, number, number2, number3, number4, number5, number6, number7);
        }
        catch (SocketException se)
        {
            if (this.config.DebugPacket.ShowCatchedException > Config.DebugPacketSettings.CatchedException.Uncommon)
            {
                TShockAPI.TShock.Log.ConsoleError($"{se}");
            }
            throw;
        }
        catch (Exception ex)
        {
            if (this.config.DebugPacket.ShowCatchedException > Config.DebugPacketSettings.CatchedException.None)
            {
                TShockAPI.TShock.Log.ConsoleError($"{ex}");
            }
            throw;
        }
    }
}