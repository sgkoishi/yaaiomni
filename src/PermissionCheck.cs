using System.Diagnostics;
using TerrariaApi.Server;
using TShockAPI;

namespace Chireiden.TShock.Omni;

public partial class Plugin : TerrariaPlugin
{
    public record PermissionCheckHistory(string Permission, DateTime Time, bool Result, StackTrace? Trace);

    private bool Hook_HasPermission(Func<TSPlayer, string, bool> orig, TSPlayer player, string permission)
    {
        var result = orig(player, permission);
        if (this.config.Permission.Log.DoLog)
        {
            if (player.GetData<Queue<PermissionCheckHistory>>(Consts.DataKey.PermissionHistory) == null)
            {
                player.SetData(Consts.DataKey.PermissionHistory, new Queue<PermissionCheckHistory>());
            }
            var history = player.GetData<Queue<PermissionCheckHistory>>(Consts.DataKey.PermissionHistory);
            var now = DateTime.Now;
            var ll = this.config.Permission.Log.LogCount;
            if (!this.config.Permission.Log.LogDuplicate)
            {
                lock (history)
                {
                    foreach (var item in history)
                    {
                        if (item.Permission == permission && (item.Time - now).TotalSeconds < this.config.Permission.Log.LogDistinctTime)
                        {
                            return result;
                        }
                    }
                }
            }
            var entry = new PermissionCheckHistory(permission, now, result, this.config.Permission.Log.LogStackTrace ? new StackTrace() : null);
            lock (history)
            {
                if (ll > 0 && history.Count == ll)
                {
                    history.Dequeue();
                }
                history.Enqueue(entry);
            }
        }
        return result;
    }

    private void Command_PermissionCheck(CommandArgs args)
    {
        Queue<PermissionCheckHistory> list;
        var existing = args.Player.GetData<Queue<PermissionCheckHistory>>(Consts.DataKey.PermissionHistory);
        if (existing != null)
        {
            lock (existing)
            {
                list = new Queue<PermissionCheckHistory>(existing);
            }
        }
        else
        {
            list = new();
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
