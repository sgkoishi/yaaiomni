using System.Diagnostics;
using TerrariaApi.Server;
using TShockAPI;

namespace Chireiden.TShock.Omni;

public partial class Plugin : TerrariaPlugin
{
    private bool Detour_HasPermission(Func<TSPlayer, string, bool> orig, TSPlayer player, string permission)
    {
        var result = orig(player, permission);
        var strgy = this.config.Permission.Log;
        if (strgy.Enabled)
        {
            var history = this[player].PermissionHistory;
            var now = DateTime.Now;
            if (!strgy.LogDuplicate)
            {
                lock (history)
                {
                    foreach (var item in history)
                    {
                        if (item.Permission == permission && (item.Time - now).TotalSeconds < strgy.LogDistinctTime)
                        {
                            return result;
                        }
                    }
                }
            }
            var entry = new AttachedData.PermissionCheckHistory(permission, now, result, strgy.LogStackTrace ? new StackTrace() : null);
            lock (history)
            {
                if (strgy.LogCount > 0 && history.Count == strgy.LogCount)
                {
                    history.Dequeue();
                }
                history.Enqueue(entry);
            }
        }
        return result;
    }

    [Command("Whynot", "chireiden.omni.whynot", "whynot", AllowServer = false)]
    private void Command_PermissionCheck(CommandArgs args)
    {
        var list = this[args.Player].PermissionHistory.ToList();

        if (args.Parameters.Contains("-t"))
        {
            list = list.Where(x => x.Result).ToList();
        }
        else if (args.Parameters.Contains("-f"))
        {
            list = list.Where(x => !x.Result).ToList();
        }

        if (list.Count == 0)
        {
            args.Player.SendInfoMessage("No permission check history found.");
            return;
        }

        args.Player.SendInfoMessage("Permission check history:");
        var detailed = args.Parameters.Contains("-v") && args.Player.HasPermission(LegacyConsts.Permissions.Admin.DetailedPermissionStackTrace);

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