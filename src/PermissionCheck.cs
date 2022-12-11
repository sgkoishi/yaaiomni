using MonoMod.RuntimeDetour;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using TerrariaApi.Server;
using TShockAPI;

namespace Chireiden.TShock.Omni;

public partial class Plugin : TerrariaPlugin
{
    public record PermissionCheckHistory(string Permission, DateTime Time, bool Result, StackTrace? Trace);
    private readonly IDetour _TSPlayerHasPermissionDetour;
    private readonly ConditionalWeakTable<TSPlayer, Queue<PermissionCheckHistory>> _permissions = new();

    public bool HasPermission(TSPlayer player, string permission)
    {
        var result = (bool) this._TSPlayerHasPermissionDetour.GenerateTrampoline().Invoke(player, new object[] { permission })!;
        if (this.config.Permission.Log.DoLog)
        {
            var history = this._permissions.GetOrCreateValue(player);
            var now = DateTime.Now;
            var ll = this.config.Permission.Log.LogCount;
            lock (this._permissions)
            {
                if (!this.config.Permission.Log.LogDuplicate)
                {
                    foreach (var item in history)
                    {
                        if (item.Permission == permission && (item.Time - now).TotalSeconds < this.config.Permission.Log.LogDistinctTime)
                        {
                            return result;
                        }
                    }
                }
                if (ll > 0 && history.Count == ll)
                {
                    history.Dequeue();
                }
                var entry = new PermissionCheckHistory(permission, now, result, this.config.Permission.Log.LogStackTrace ? new StackTrace() : null);
                history.Enqueue(entry);
            }
        }
        return result;
    }

    private void QueryPermissionCheck(CommandArgs args)
    {
        var list = new List<PermissionCheckHistory>();
        lock (this._permissions)
        {
            var l = this._permissions.GetOrCreateValue(args.Player);
            list = l.ToList();
        }

        if (list.Count == 0)
        {
            args.Player.SendInfoMessage("No permission check history found.");
            return;
        }

        args.Player.SendInfoMessage("Permission check history:");
        var detailed = args.Parameters.Contains("-t") && args.Player.HasPermission("chireiden.omni.whynot.detailed");

        foreach (var item in list)
        {
            if (item.Result)
            {
                args.Player.SendSuccessMessage($"{item.Permission} @ {item.Time.ToString(this.config.DateTimeFormat)}");
            }
            else
            {
                args.Player.SendErrorMessage($"{item.Permission} @ {item.Time.ToString(this.config.DateTimeFormat)}");
            }
            if (detailed && item.Trace != null)
            {
                args.Player.SendInfoMessage(item.Trace.ToString());
            }
        }
    }
}
