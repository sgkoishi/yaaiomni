using TerrariaApi.Server;

namespace Chireiden.TShock.Omni;

public partial class Plugin : TerrariaPlugin
{
    private string? _pendingDefault;
    private PendingConfig _pendingConfig = PendingConfig.None;
    private string MMHook_CliConfig_LanguageText(On.Terraria.Localization.Language.orig_GetTextValue_string orig, string key)
    {
        if (this.config.Enhancements.CLIoverConfig && this._pendingConfig != PendingConfig.Done)
        {
            if (key == "CLI.SetInitialMaxPlayers")
            {
                this._pendingDefault = $"{TShockAPI.TShock.Config.Settings.MaxSlots}";
                this._pendingConfig = PendingConfig.MaxPlayers;
                return orig(key).Replace("16", this._pendingDefault);
            }
            if (key == "CLI.SetInitialPort")
            {
                this._pendingDefault = $"{TShockAPI.TShock.Config.Settings.ServerPort}";
                this._pendingConfig = PendingConfig.Port;
                return orig(key).Replace("7777", this._pendingDefault);
            }
            if (key == "CLI.EnterServerPassword")
            {
                this._pendingDefault = $"{TShockAPI.TShock.Config.Settings.ServerPassword}";
                this._pendingConfig = PendingConfig.Password;
                var o = orig(key);
                return $"{(o.Contains('(') ? o[..o.IndexOf('(')] : o)}(Enter -> config.json):";
            }
        }
        return orig(key);
    }

    private string MMHook_CliConfig_ReadLine(On.Terraria.Main.orig_ReadLineInput orig)
    {
        var o = orig();
        if (this.config.Enhancements.CLIoverConfig)
        {
            if (this._pendingDefault != null)
            {
                if (string.IsNullOrEmpty(o))
                {
                    var value = this._pendingDefault;
                    this._pendingDefault = null;
                    this._pendingConfig = PendingConfig.None;
                    return value;
                }
                switch (this._pendingConfig)
                {
                    case PendingConfig.MaxPlayers:
                    {
                        var i = Convert.ToInt32(o);
                        this._pendingConfig = PendingConfig.None;
                        if (i < 1 || i > 255)
                        {
                            var value = this._pendingDefault;
                            this._pendingDefault = null;
                            return value;
                        }
                        TShockAPI.TShock.Config.Settings.MaxSlots = i;
                        return o;
                    }
                    case PendingConfig.Port:
                    {
                        var i = Convert.ToInt32(o);
                        this._pendingConfig = PendingConfig.None;
                        if (i < 1 || i > 65535)
                        {
                            var value = this._pendingDefault;
                            this._pendingDefault = null;
                            return value;
                        }
                        TShockAPI.TShock.Config.Settings.ServerPort = i;
                        return o;
                    }
                    case PendingConfig.Password:
                    {
                        this._pendingConfig = PendingConfig.Done;
                        TShockAPI.TShock.Config.Settings.ServerPassword = this._pendingDefault;
                        this._pendingDefault = null;
                        break;
                    }
                }
            }
        }
        return o;
    }

    internal enum PendingConfig
    {
        None,
        MaxPlayers,
        Port,
        Password,
        Done,
    }
}