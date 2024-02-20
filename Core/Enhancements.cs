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

    internal Dictionary<string, string> _localizedCommandsMap = [];
    private readonly System.Runtime.CompilerServices.ConditionalWeakTable<TShockAPI.Command, List<string>> _addedAlias = new();
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

    private void MMHook_Mitigation_I18nCommand(On.Terraria.Initializers.ChatInitializer.orig_Load orig)
    {
        // Pryaxis/TShock#2914
        Terraria.UI.Chat.ChatManager.Commands._localizedCommands.Clear();
        orig();
        this.RefreshLocalizedCommandAliases();
    }

    private void RefreshLocalizedCommandAliases()
    {
        if (this.config.Soundness.Value.UseEnglishCommand)
        {
            var currentLanguage = Terraria.Localization.LanguageManager.Instance.ActiveCulture;
            Terraria.Localization.LanguageManager.Instance.LoadLanguage(Terraria.Localization.GameCulture.FromCultureName(Terraria.Localization.GameCulture.CultureName.English));
            var items = Terraria.UI.Chat.ChatManager.Commands._localizedCommands.ToList();
            Terraria.UI.Chat.ChatManager.Commands._localizedCommands.Clear();
            foreach (var (key, value) in items)
            {
                Terraria.UI.Chat.ChatManager.Commands._localizedCommands[new Terraria.Localization.LocalizedText(key.Key, key.Value)] = value;
            }
            var chatCommands = items.ToDictionary(kvp => kvp.Key.Key, kvp => kvp.Key.Value);
            var cliCommands = Terraria.Localization.LanguageManager.Instance._localizedTexts
                .Where(kvp => kvp.Key.StartsWith("CLI.") && kvp.Key.EndsWith("_Command"))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Value);
            Terraria.Localization.LanguageManager.Instance.LoadLanguage(currentLanguage);
            this._localizedCommandsMap.Clear();
            foreach (var c in chatCommands)
            {
                // Chat commands start with / and we don't want that
                this._localizedCommandsMap[Terraria.Localization.Language.GetText(c.Key).Value[1..]] = c.Value[1..];
            }
            foreach (var c in cliCommands)
            {
                this._localizedCommandsMap[Terraria.Localization.Language.GetText(c.Key).Value] = c.Value;
            }
        }
        if (this.config.Soundness.Value.AllowVanillaLocalizedCommand)
        {
            foreach (var kvp in this._addedAlias)
            {
                if (kvp.Key.Names.Count > 1)
                {
                    foreach (var name in kvp.Value)
                    {
                        kvp.Key.Names.Remove(name);
                    }
                    kvp.Value.Clear();
                }
            }
            this._addedAlias.Clear();
            foreach (var command in TShockAPI.Commands.ChatCommands)
            {
                foreach (var bc in this._localizedCommandsMap)
                {
                    if (command.HasAlias(bc.Value) && !command.HasAlias(bc.Key))
                    {
                        command.Names.Insert(1, bc.Key);
                        var l = this._addedAlias.GetOrCreateValue(command);
                        l.Add(bc.Key);
                    }
                }
            }
        }
    }

    private void Detour_Socket_StartDualMode(Action<System.Net.Sockets.TcpListener, int> orig, System.Net.Sockets.TcpListener listener, int backlog)
    {
        if (this.config.Enhancements.Value.IPv6DualStack)
        {
            try
            {
                listener.Server.DualMode = true;
                TShockAPI.TShock.Log.ConsoleInfo("Dual stack enabled.");
            }
            catch (Exception e)
            {
                Utils.ShowError($"Failed to enable dual stack on {listener.LocalEndpoint}: {e}");
            }
        }
        orig(listener, backlog);
    }
}