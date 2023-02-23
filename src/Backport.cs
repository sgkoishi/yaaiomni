using MonoMod.Cil;
using System.Reflection;
using TerrariaApi.Server;

namespace Chireiden.TShock.Omni;

public partial class Plugin : TerrariaPlugin
{
    private bool Detour_Backport_2894(Func<TShockAPI.DB.CharacterManager, TShockAPI.TSPlayer, bool, bool> orig,
        TShockAPI.DB.CharacterManager self, TShockAPI.TSPlayer player, bool fromCommand)
    {
        // FIXME: This is a backport of Pryaxis/TShock#2894
        return player.State >= 10 && orig(self, player, fromCommand);
    }

    private void Backports()
    {
        var bouncer = Utils.TShockType("Bouncer");
        if (bouncer?.GetField("NPCAddBuffTimeMax", _bfany)?.GetValue(null) is Dictionary<int, int> npcAddBuffTimeMax)
        {
            if (npcAddBuffTimeMax[Terraria.ID.BuffID.CursedInferno] == 420)
            {
                npcAddBuffTimeMax[Terraria.ID.BuffID.CursedInferno] = 600;
            }
        }

        // FIXME: This is a backport of Pryaxis/TShock#2892
        this.ILHook(
            nameof(this.ILHook_Backport_2892),
            typeof(TShockAPI.Utils).GetMethod(nameof(TShockAPI.Utils.GetItemFromTag), _bfany)!,
            this.ILHook_Backport_2892);
    }

    private void ILHook_Backport_2892(ILContext context)
    {
        if ((context.Body?.Instructions[0]?.Operand?.ToString() ?? "").Contains(@"(?:\/s(?<Stack>\d{1,4}))"))
        {
            context.Body!.Instructions[0].Operand = @"\[i(tem)?(?:\/s(?<Stack>\d{1,4}))?(?:\/p(?<Prefix>\d{1,3}))?:(?<NetID>-?\d{1,4})\]";
        }
    }
}
