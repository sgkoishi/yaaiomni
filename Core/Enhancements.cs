namespace Chireiden.TShock.Omni;

public partial class Plugin
{
    private void MMHook_MemoryTrim_DisplayDoll(On.Terraria.GameContent.Tile_Entities.TEDisplayDoll.orig_ctor orig, Terraria.GameContent.Tile_Entities.TEDisplayDoll self)
    {
        orig(self);
        if (this.config.Enhancements.Value.TrimMemory)
        {
            self._dollPlayer = null;
        }
    }

    private void MMHook_MemoryTrim_HatRack(On.Terraria.GameContent.Tile_Entities.TEHatRack.orig_ctor orig, Terraria.GameContent.Tile_Entities.TEHatRack self)
    {
        orig(self);
        if (this.config.Enhancements.Value.TrimMemory)
        {
            self._dollPlayer = null;
        }
    }

    private async Task Detour_UpdateCheckAsync(Func<TShockAPI.UpdateManager, object, Task> orig, TShockAPI.UpdateManager um, object state)
    {
        var flag = this.config.Enhancements.Value.SuppressUpdate.Value;
        if (flag == Config.EnhancementsSettings.UpdateOptions.Disabled)
        {
            return;
        }
        try
        {
            await orig(um, state);
            return;
        }
        catch when (flag is Config.EnhancementsSettings.UpdateOptions.Silent)
        {
            return;
        }
    }

    private void TAHook_NameCollision(TerrariaApi.Server.NameCollisionEventArgs args)
    {
        if (args.Handled)
        {
            return;
        }
        var ip = TShockAPI.TShock.Utils.GetRealIP(Terraria.Netplay.Clients[args.Who].Socket.GetRemoteAddress().ToString());
        var player = Utils.ActivePlayers.FirstOrDefault(p => p.Name == args.Name && p.Index != args.Who);
        var account = TShockAPI.TShock.UserAccounts.GetUserAccountByName(args.Name);
        var knownIPs = Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(account?.KnownIps ?? "[]")!;
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
                throw new System.Runtime.CompilerServices.SwitchExpressionException($"Unexpected option {this.config.Enhancements.Value.NameCollision.Value}");
        }
        if (first)
        {
            Terraria.NetMessage.BootPlayer(player!.Index, Terraria.Localization.NetworkText.FromKey(Terraria.Lang.mp[5].Key, args.Name));
        }
        if (second)
        {
            Terraria.NetMessage.BootPlayer(args.Who, Terraria.Localization.NetworkText.FromKey(Terraria.Lang.mp[5].Key, args.Name));
        }
    }

    private string Detour_HelpAliases(Func<object, TShockAPI.Command, string> orig, object _instance, TShockAPI.Command command)
    {
        var ac = this.config.Enhancements.Value.ShowCommandAlias.Value;
        if (ac == 0 || command.Names.Count == 1)
        {
            return orig(_instance, command);
        }
        var aliases = string.Join(", ", command.Names.Skip(1).Take(ac).Select(x => TShockAPI.Commands.Specifier + x));
        return $"{TShockAPI.Commands.Specifier}{command.Name} ({aliases})";
    }
}