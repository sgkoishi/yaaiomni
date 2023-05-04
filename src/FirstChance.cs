using System.Diagnostics;
using System.Runtime.ExceptionServices;
using TerrariaApi.Server;

namespace Chireiden.TShock.Omni;

public partial class Plugin : TerrariaPlugin
{
    private readonly ThreadLocal<int> inFirstChance = new ThreadLocal<int>(() => 0);
    private readonly HashSet<string> exceptions = new HashSet<string>();

    private void FirstChanceExceptionHandler(object? sender, FirstChanceExceptionEventArgs args)
    {
        if (!this.config.LogFirstChance)
        {
            return;
        }

        if (this.inFirstChance.Value >= 5)
        {
            return;
        }

        try
        {
            this.inFirstChance.Value++;
            var trace = new StackTrace(true);
            if (this.exceptions.Add(trace.ToString()))
            {
                TShockAPI.TShock.Log.ConsoleError($"New First Chance: {trace}");
            }
        }
        catch
        {
        }
        finally
        {
            this.inFirstChance.Value--;
        }
    }
}