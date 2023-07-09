using Newtonsoft.Json;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.Localization;
using TerrariaApi.Server;

namespace Chireiden.TShock.Omni;

public partial class Plugin
{
    private void TAHook_NameCollision(NameCollisionEventArgs args)
    {
        if (args.Handled)
        {
            return;
        }
        var ip = TShockAPI.TShock.Utils.GetRealIP(Netplay.Clients[args.Who].Socket.GetRemoteAddress().ToString());
        var player = Utils.ActivePlayers.FirstOrDefault(p => p.Name == args.Name && p.Index != args.Who);
        var account = TShockAPI.TShock.UserAccounts.GetUserAccountByName(args.Name);
        var knownIPs = JsonConvert.DeserializeObject<List<string>>(account?.KnownIps ?? "[]")!;
        var first = false;
        var second = false;
        switch (this.config.Enhancements.Value.NameCollision.Value)
        {
            case Config.EnhancementsSettings.NameCollisionAction.First:
                first = true;
                args.Handled = true;
                break;
            case Config.EnhancementsSettings.NameCollisionAction.Second:
                second = true;
                args.Handled = true;
                break;
            case Config.EnhancementsSettings.NameCollisionAction.Both:
                first = true;
                second = true;
                args.Handled = true;
                break;
            case Config.EnhancementsSettings.NameCollisionAction.None:
                args.Handled = true;
                break;
            case Config.EnhancementsSettings.NameCollisionAction.Known:
                if (!knownIPs.Contains(ip))
                {
                    second = true;
                }
                else if (player != null && !knownIPs.Contains(player.IP) && !player.IsLoggedIn)
                {
                    first = true;
                }
                else
                {
                    second = true;
                }
                args.Handled = true;
                break;
            case Config.EnhancementsSettings.NameCollisionAction.Unhandled:
                return;
            default:
                throw new SwitchExpressionException($"Unexpected option {this.config.Enhancements.Value.NameCollision.Value}");
        }
        if (first)
        {
            NetMessage.BootPlayer(player!.Index, NetworkText.FromKey(Lang.mp[5].Key, args.Name));
        }
        if (second)
        {
            NetMessage.BootPlayer(args.Who, NetworkText.FromKey(Lang.mp[5].Key, args.Name));
        }
    }
}