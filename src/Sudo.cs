using System.Diagnostics;
using System.Runtime.CompilerServices;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace Chireiden.TShock.Omni;

public partial class Plugin : TerrariaPlugin
{
    private int _globalPermissionBypass = 0;
    internal class IntObject
    {
        public int Value;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public void RunWithoutPermissionChecks(Action action, TSPlayer? player = null)
    {
        if (player != null && player.RealPlayer)
        {
            var iobj = player.GetOrCreatePlayerAttachedData<IntObject>(Consts.DataKey.PermissionBypass);
            Interlocked.Increment(ref iobj.Value);
        }
        else
        {
            Interlocked.Increment(ref this._globalPermissionBypass);
        }
        try
        {
            action();
        }
        finally
        {
            if (player != null && player.RealPlayer)
            {
                var iobj = player.GetOrCreatePlayerAttachedData<IntObject>(Consts.DataKey.PermissionBypass);
                Interlocked.Decrement(ref iobj.Value);
            }
            else
            {
                Interlocked.Decrement(ref this._globalPermissionBypass);
            }
        }
    }

    private void Hook_Sudo_OnPlayerPermission(PlayerPermissionEventArgs args)
    {
        var flag = false;
        if (args.Player.GetOrCreatePlayerAttachedData<IntObject>(Consts.DataKey.PermissionBypass).Value > 0)
        {
            flag = true;
        }

        if (this._globalPermissionBypass > 0)
        {
            flag = true;
        }

        if (!flag)
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
