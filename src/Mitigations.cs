using TerrariaApi.Server;

namespace Chireiden.TShock.Omni;

public partial class Plugin : TerrariaPlugin
{
    internal static class Mitigations
    {
        /// <summary>
        /// Mobile (PE) client keep (likely every frame) send PlayerSlot packet to the server if
        /// any item exists in non-active loadout.
        ///
        /// Cause lag.
        ///
        /// This will silently proceed the packet without boardcasting it.
        /// </summary>
        public static bool HandleInventorySlotPE(byte player, Span<byte> data)
        {
            if (data.Length != 8)
            {
                return true;
            }

            if (data[0] != player)
            {
                return true;
            }

            var slot = BitConverter.ToInt16(data.Slice(1, 2));

            // Don't handle if we don't care
            if (slot < Terraria.ID.PlayerItemSlotID.Loadout1_Armor_0)
            {
                return false;
            }
            // For future compatibility
            if (slot > Terraria.ID.PlayerItemSlotID.Loadout3_Dye_0 + 10)
            {
                return false;
            }

            slot -= (short) Terraria.ID.PlayerItemSlotID.Loadout1_Armor_0;
            var loadoutIndex = slot / 30;
            if (loadoutIndex == Terraria.Main.player[player].CurrentLoadoutIndex)
            {
                return false;
            }

            slot %= 30;
            var stack = BitConverter.ToInt16(data.Slice(3, 2));
            var prefix = data[5];
            var type = BitConverter.ToInt16(data.Slice(6, 2));
            const short ArmorLength = 20;
            var array = slot >= ArmorLength
                ? Terraria.Main.player[player].Loadouts[loadoutIndex].Dye
                : Terraria.Main.player[player].Loadouts[loadoutIndex].Armor;
            var index = slot % ArmorLength;
            array[index] = new Terraria.Item();
            array[index].netDefaults(type);
            array[index].stack = stack;
            array[index].Prefix(prefix);
            // Handled, stop broadcasting
            return true;
        }
    }

    private void Mitigation_GetData(object? sender, OTAPI.Hooks.MessageBuffer.GetDataEventArgs e)
    {
        if (e.Result == OTAPI.HookResult.Cancel)
        {
            return;
        }

        var mitigation = this.config.Mitigation;
        if (!mitigation.Enabled)
        {
            return;
        }

        switch (e.PacketId)
        {
            case (int) PacketTypes.PlayerSlot:
                if (mitigation.InventorySlotPE)
                {
                    if (Mitigations.HandleInventorySlotPE((byte) e.Instance.whoAmI, e.Instance.readBuffer.AsSpan(e.ReadOffset, e.Length - 1)))
                    {
                        this.Statistics.MitigationSlotPE++;
                        e.Result = OTAPI.HookResult.Cancel;
                    }
                    else
                    {
                        this.Statistics.MitigationSlotPEAllowed++;
                    }
                }
                break;
            default:
                break;
        }
    }
}
