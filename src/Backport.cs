using MonoMod.Cil;
using TerrariaApi.Server;

namespace Chireiden.TShock.Omni;

public partial class Plugin
{
    #region MyRegion
    [Obsolete]
    private void ILHook_Backport_2892(ILContext context)
    {
        if (context.Body?.Instructions[0]?.Operand?.ToString()?.Contains(@"(?:\/s(?<Stack>\d{1,3}))") == true)
        {
            context.Body!.Instructions[0].Operand = @"\[i(tem)?(?:\/s(?<Stack>\d{1,4}))?(?:\/p(?<Prefix>\d{1,3}))?:(?<NetID>-?\d{1,4})\]";
        }
    }

    [Obsolete]
    private bool Detour_Backport_2894(Func<TShockAPI.DB.CharacterManager, TShockAPI.TSPlayer, bool, bool> orig,
        TShockAPI.DB.CharacterManager self, TShockAPI.TSPlayer player, bool fromCommand)
    {
        return player.State >= 10 && orig(self, player, fromCommand);
    }

    [Obsolete("Pre 5.2.0")]
    public void Run()
    {
        this.Detour(
            nameof(this.Detour_Backport_2894),
            typeof(TShockAPI.DB.CharacterManager)
                .GetMethod(nameof(TShockAPI.DB.CharacterManager.InsertPlayerData), _bfany)!,
            this.Detour_Backport_2894);

        this.ILHook(
            nameof(this.ILHook_Backport_2892),
            typeof(TShockAPI.Utils).GetMethod(nameof(TShockAPI.Utils.GetItemFromTag), _bfany)!,
            this.ILHook_Backport_2892);
    }
    #endregion

private void Detour_Backport_2934(Action orig)
    {
        orig();
        if (!Terraria.Netplay.HasClients)
        {
            if (this._tickCheck.Tick != -1)
            {
                var (Tick, Time) = this._tickCheck;
                this._tickCheck = (-1, DateTime.MinValue);
                var diff = this._updateCounter - Tick;
                var time = DateTime.Now - Time;
                TShockAPI.TShock.Log.ConsoleInfo(
                    $"[Omni] {diff} ticks in {time.TotalSeconds:F2} seconds ({diff / time.TotalSeconds:F2} tps)");
            }
        }
        if (ServerApi.ForceUpdate)
        {
            Terraria.Netplay.HasClients = true;
        }
    }

    private void Backport_Inferno()
    {
        var bouncer = Utils.TShockType("Bouncer");
        if (bouncer?.GetField("NPCAddBuffTimeMax", _bfany)?.GetValue(null) is Dictionary<int, int> npcAddBuffTimeMax)
        {
            if (npcAddBuffTimeMax[Terraria.ID.BuffID.CursedInferno] == 420)
            {
                npcAddBuffTimeMax[Terraria.ID.BuffID.CursedInferno] = 600;
            }
        }
    }

    private void Backports()
    {
        this.Backport_Inferno();

        this.Detour(
            nameof(this.Detour_Backport_2934),
            typeof(Terraria.Netplay).GetMethod(nameof(Terraria.Netplay.UpdateConnectedClients), _bfany)!,
            this.Detour_Backport_2934);
    }
}