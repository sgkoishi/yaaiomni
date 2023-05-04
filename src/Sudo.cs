using System.Diagnostics;
using System.Runtime.CompilerServices;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace Chireiden.TShock.Omni;

public partial class Plugin : TerrariaPlugin
{
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public void RunWithoutPermissionChecks(Action action, TSPlayer? player = null)
    {
        if (player != null && player.RealPlayer)
        {
            Interlocked.Increment(ref this[player].PermissionBypass);
        }
        else
        {
            Interlocked.Increment(ref this[TSPlayer.Server].PermissionBypass);
        }
        try
        {
            action();
        }
        finally
        {
            if (player != null && player.RealPlayer)
            {
                Interlocked.Decrement(ref this[player].PermissionBypass);
            }
            else
            {
                Interlocked.Decrement(ref this[TSPlayer.Server].PermissionBypass);
            }
        }
    }

    private void TSHook_Sudo_OnPlayerPermission(PlayerPermissionEventArgs args)
    {
        if (this[args.Player].PermissionBypass <= 0 && this[TSPlayer.Server].PermissionBypass <= 0)
        {
            return;
        }

        var trace = new StackTrace();
        var bp = ((Delegate) this.RunWithoutPermissionChecks).Method;
        foreach (var frame in trace.GetFrames())
        {
            var method = frame.GetMethod();
            if (method is null)
            {
                continue;
            }
            if (method.Equals(bp))
            {
                args.Result = PermissionHookResult.Granted;
                return;
            }
        }
    }
}