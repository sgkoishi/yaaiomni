using System.Diagnostics;
using System.Runtime.ExceptionServices;

namespace Chireiden.TShock.Omni;

public partial class Plugin
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
            var v = args.Exception.ToString();
            if (this.exceptions.Add(v))
            {
                this.ShowError($"New First Chance: {v}");
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