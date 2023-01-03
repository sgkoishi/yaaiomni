using Newtonsoft.Json;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.Localization;
using TerrariaApi.Server;

namespace Chireiden.TShock.Omni;

public partial class Plugin : TerrariaPlugin
{
    private void Hook_NameCollision(NameCollisionEventArgs args)
    {
        var ip = TShockAPI.TShock.Utils.GetRealIP(Netplay.Clients[args.Who].Socket.GetRemoteAddress().ToString());
        var player = TShockAPI.TShock.Players.First(p => p != null && p.Name == args.Name && p.Index != args.Who);
        var account = TShockAPI.TShock.UserAccounts.GetUserAccountByName(args.Name);
        var knownIPs = JsonConvert.DeserializeObject<List<string>>(account?.KnownIps ?? "[]")!;
        var first = false;
        var second = false;
        switch (this.config.NameCollision)
        {
            case Config.NameCollisionAction.First:
                first = true;
                args.Handled = true;
                break;
            case Config.NameCollisionAction.Second:
                second = true;
                args.Handled = true;
                break;
            case Config.NameCollisionAction.Both:
                first = true;
                second = true;
                args.Handled = true;
                break;
            case Config.NameCollisionAction.None:
                args.Handled = true;
                break;
            case Config.NameCollisionAction.Known:
                if (!knownIPs.Contains(ip))
                {
                    second = true;
                }
                else if (!knownIPs.Contains(player.IP) && !player.IsLoggedIn)
                {
                    first = true;
                }
                else
                {
                    second = true;
                }
                args.Handled = true;
                break;
            case Config.NameCollisionAction.Unhandled:
                return;
            default:
                throw new SwitchExpressionException($"Unexpected option {this.config.NameCollision}");
        }
        if (first)
        {
            NetMessage.BootPlayer(player.Index, NetworkText.FromKey(Lang.mp[5].Key, args.Name));
        }
        if (second)
        {
            NetMessage.BootPlayer(args.Who, NetworkText.FromKey(Lang.mp[5].Key, args.Name));
        }
    }
}
