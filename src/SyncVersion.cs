using Terraria;
using TerrariaApi.Server;

namespace Chireiden.TShock.Omni;

public partial class Plugin : TerrariaPlugin
{
    private static readonly byte[] _versionPacket = new byte[] {
        (byte) PacketTypes.ConnectRequest,
        (byte) (8 + Main.curRelease.ToString().Length),
        (byte) 'T', (byte) 'e', (byte) 'r', (byte) 'r', (byte) 'a', (byte) 'r', (byte) 'i', (byte) 'a'
    };
    private static readonly byte[] _versionCode = Main.curRelease.ToString().Select(Convert.ToByte).ToArray();

    private void MMHook_PatchVersion_GetData(On.Terraria.MessageBuffer.orig_GetData orig, MessageBuffer self, int start, int length, out int messageType)
    {
        if (self.readBuffer[start] == 1 && length == 13)
        {
            if (self.readBuffer.AsSpan(start, 11).SequenceEqual(_versionPacket))
            {
                if (this.config.SyncVersion)
                {
                    Buffer.BlockCopy(_versionCode, 0, self.readBuffer, start + 11, 3);
                }
            }
        }
        orig(self, start, length, out messageType);
    }
}
