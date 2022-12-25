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
            var stack = BitConverter.ToInt16(data.Slice(3, 2));
            var prefix = data[5];
            var type = BitConverter.ToInt16(data.Slice(6, 2));

            var p = Terraria.Main.player[player];
            var existingItem = slot switch
            {
                short when Terraria.ID.PlayerItemSlotID.Loadout3_Dye_0 + 10 > slot && slot >= Terraria.ID.PlayerItemSlotID.Loadout3_Dye_0
                    => p.Loadouts[2].Dye[slot - Terraria.ID.PlayerItemSlotID.Loadout3_Dye_0],
                short when Terraria.ID.PlayerItemSlotID.Loadout3_Dye_0 > slot && slot >= Terraria.ID.PlayerItemSlotID.Loadout3_Armor_0
                    => p.Loadouts[2].Armor[slot - Terraria.ID.PlayerItemSlotID.Loadout3_Armor_0],
                short when Terraria.ID.PlayerItemSlotID.Loadout3_Armor_0 > slot && slot >= Terraria.ID.PlayerItemSlotID.Loadout2_Dye_0
                    => p.Loadouts[1].Dye[slot - Terraria.ID.PlayerItemSlotID.Loadout2_Dye_0],
                short when Terraria.ID.PlayerItemSlotID.Loadout2_Dye_0 > slot && slot >= Terraria.ID.PlayerItemSlotID.Loadout2_Armor_0
                    => p.Loadouts[1].Armor[slot - Terraria.ID.PlayerItemSlotID.Loadout2_Armor_0],
                short when Terraria.ID.PlayerItemSlotID.Loadout2_Armor_0 > slot && slot >= Terraria.ID.PlayerItemSlotID.Loadout1_Dye_0
                    => p.Loadouts[0].Dye[slot - Terraria.ID.PlayerItemSlotID.Loadout1_Dye_0],
                short when Terraria.ID.PlayerItemSlotID.Loadout1_Dye_0 > slot && slot >= Terraria.ID.PlayerItemSlotID.Loadout1_Armor_0
                    => p.Loadouts[0].Armor[slot - Terraria.ID.PlayerItemSlotID.Loadout1_Armor_0],
                short when Terraria.ID.PlayerItemSlotID.Loadout1_Armor_0 > slot && slot >= Terraria.ID.PlayerItemSlotID.Bank4_0
                    => p.bank4.item[slot - Terraria.ID.PlayerItemSlotID.Bank4_0],
                short when Terraria.ID.PlayerItemSlotID.Bank4_0 > slot && slot >= Terraria.ID.PlayerItemSlotID.Bank3_0
                    => p.bank3.item[slot - Terraria.ID.PlayerItemSlotID.Bank3_0],
                short when Terraria.ID.PlayerItemSlotID.Bank3_0 > slot && slot >= Terraria.ID.PlayerItemSlotID.TrashItem
                    => p.trashItem,
                short when Terraria.ID.PlayerItemSlotID.TrashItem > slot && slot >= Terraria.ID.PlayerItemSlotID.Bank2_0
                    => p.bank2.item[slot - Terraria.ID.PlayerItemSlotID.Bank2_0],
                short when Terraria.ID.PlayerItemSlotID.Bank2_0 > slot && slot >= Terraria.ID.PlayerItemSlotID.Bank1_0
                    => p.bank.item[slot - Terraria.ID.PlayerItemSlotID.Bank1_0],
                short when Terraria.ID.PlayerItemSlotID.Bank1_0 > slot && slot >= Terraria.ID.PlayerItemSlotID.MiscDye0
                    => p.miscDyes[slot - Terraria.ID.PlayerItemSlotID.MiscDye0],
                short when Terraria.ID.PlayerItemSlotID.MiscDye0 > slot && slot >= Terraria.ID.PlayerItemSlotID.Misc0
                    => p.miscEquips[slot - Terraria.ID.PlayerItemSlotID.Misc0],
                short when Terraria.ID.PlayerItemSlotID.Misc0 > slot && slot >= Terraria.ID.PlayerItemSlotID.Dye0
                    => p.dye[slot - Terraria.ID.PlayerItemSlotID.Dye0],
                short when Terraria.ID.PlayerItemSlotID.Dye0 > slot && slot >= Terraria.ID.PlayerItemSlotID.Armor0
                    => p.armor[slot - Terraria.ID.PlayerItemSlotID.Armor0],
                short when Terraria.ID.PlayerItemSlotID.Armor0 > slot && slot >= Terraria.ID.PlayerItemSlotID.Inventory0
                    => p.inventory[slot - Terraria.ID.PlayerItemSlotID.Inventory0],
                _ => throw new System.Runtime.CompilerServices.SwitchExpressionException($"Unexpected slot: {slot}")
            };

            if (existingItem != null)
            {
                if ((existingItem.netID == 0 || existingItem.stack == 0) && (type == 0 || stack == 0))
                {
                    return true;
                }
                if (existingItem.netID == type && existingItem.stack == stack && existingItem.prefix == prefix)
                {
                    return true;
                }
            }
            return false;
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
                    var index = e.Instance.whoAmI;
                    if (Mitigations.HandleInventorySlotPE((byte) index, e.Instance.readBuffer.AsSpan(e.ReadOffset, e.Length - 1)))
                    {
                        this.Statistics.MitigationSlotPE++;
                        var value = TShockAPI.TShock.Players[index].GetData<int>(Consts.DataKey.DetectPE);
                        if (value <= 500)
                        {
                            TShockAPI.TShock.Players[index].SetData<int>(Consts.DataKey.DetectPE, value + 1);
                            if (value == 500)
                            {
                                TShockAPI.TShock.Players[index].SetData<bool>(Consts.DataKey.IsPE, true);
                            }
                        }
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
