using MonoMod.Cil;
using TerrariaApi.Server;

namespace Chireiden.TShock.Omni;

public partial class Plugin
{
    private void Detour_Backport_2934(Action orig)
    {
        orig();
        if (ServerApi.ForceUpdate)
        {
            Terraria.Netplay.HasClients = true;
        }
    }

    private void Backport_Inferno()
    {
        // Reported via Discord https://discord.com/channels/479657350043664384/482065271297671168/1061908947151372408
        var bouncer = Utils.TShockType("Bouncer");
        if (bouncer?.GetField("NPCAddBuffTimeMax", _bfany)?.GetValue(null) is Dictionary<int, short> npcAddBuffTimeMax)
        {
            if (npcAddBuffTimeMax[Terraria.ID.BuffID.CursedInferno] == 420)
            {
                npcAddBuffTimeMax[Terraria.ID.BuffID.CursedInferno] = 600;
            }
        }
    }

    private void Backport_3005()
    {
        var bouncer = Utils.TShockType("Bouncer");
        if (bouncer?.GetField("PlayerAddBuffWhitelist", _bfany)?.GetValue(null) is Array array)
        {
            var bl = bouncer.GetNestedType("BuffLimit", _bfany)!;
            var bli = Activator.CreateInstance(bl);
            bl.GetProperty("MaxTicks", _bfany)!.SetValue(bli, 300);
            bl.GetProperty("CanBeAddedWithoutHostile", _bfany)!.SetValue(bli, true);
            bl.GetProperty("CanOnlyBeAppliedToSender", _bfany)!.SetValue(bli, true);
            array.SetValue(bli, Terraria.ID.BuffID.ParryDamageBuff);
        }
    }

    private void Backports()
    {
        this.Backport_Inferno();
        this.Backport_3005();

        this.Detour(
            nameof(this.Detour_Backport_2934),
            typeof(Terraria.Netplay).GetMethod(nameof(Terraria.Netplay.UpdateConnectedClients), _bfany),
            this.Detour_Backport_2934);
    }
}