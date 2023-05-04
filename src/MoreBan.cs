using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.DB;

namespace Chireiden.TShock.Omni;

public partial class Plugin : TerrariaPlugin
{
    private FieldInfo? _CheckBan_lambda_player;
    private bool Detour_CheckBan_IP(Func<object, KeyValuePair<int, Ban>, bool> orig, object instance, KeyValuePair<int, Ban> kvp)
    {
        static bool Match(string pattern, TSPlayer player)
        {
            if (pattern.StartsWith("namea:"))
            {
                var pt = pattern[6..];
                try
                {
                    if (!Regex.IsMatch(player.Name, pt))
                    {
                        return false;
                    }
                }
                catch (ArgumentException ex)
                {
                    TShockAPI.TShock.Log.Error($"Ban pattern {pt} is invalid: {ex.Message}");
                    return false;
                }
            }
            else if (pattern.StartsWith("ipa:"))
            {
                var addr = pattern[4..].Split('/');
                if (addr.Length != 2
                    || !IPAddress.TryParse(addr[0], out var subnetAddr)
                    || !int.TryParse(addr[1], out var subnetMask))
                {
                    TShockAPI.TShock.Log.Error($"Ban pattern {pattern} is invalid.");
                    return false;
                }

                if (!IPAddress.TryParse(player.IP, out var ip))
                {
                    return false;
                }

                var smask = 2 << subnetMask;
                if ((Utils.ToInt(ip) & smask) != (Utils.ToInt(subnetAddr) & smask))
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

            return true;
        }
        if (this._CheckBan_lambda_player == null)
        {
            this._CheckBan_lambda_player = instance.GetType().GetField("player")!;
        }
        if (this.config.Enhancements.BanPattern)
        {
            var player = ((TSPlayer) this._CheckBan_lambda_player.GetValue(instance)!)!;
            var ban = kvp.Value;
            if (Match(ban.Identifier, player))
            {
                return TShockAPI.TShock.Bans.IsValidBan(ban, player);
            }
        }
        return orig(instance, kvp);
    }
}