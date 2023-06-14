using GetText;
using System.Globalization;
using System.Reflection;
using Terraria.Localization;
using TShockAPI;

namespace Chireiden.TShock.Omni;

public partial class Plugin
{
    private CultureInfo? _targetCulture = null;
    private readonly Type _tshockI18n = Utils.TShockType("I18n");
    private MethodInfo _tscinfo = null!;

    [Command("Admin.ManageLanguage", "chireiden.omni.setlang", "setlang")]
    private void Command_Lang(CommandArgs args)
    {
        this._tscinfo ??= this._tshockI18n
           .GetProperty("TranslationCultureInfo", BindingFlags.NonPublic | BindingFlags.Static)!
           .GetGetMethod(true)!;

        if (args.Parameters.Count == 0)
        {
            args.Player.SendInfoMessage($"Current TShock Lang: {this._targetCulture ?? this._tscinfo.Invoke(null, new object[0])}");
            args.Player.SendInfoMessage($"Current Game Lang: {LanguageManager.Instance.ActiveCulture.CultureInfo}");
            return;
        }

        var setGameLang = args.Parameters.Contains("-g");
        var setTsLang = args.Parameters.Contains("-t");
        var remaining = args.Parameters.Where(p => p != "-t" && p != "-g").ToList();

        if (!setGameLang && !setTsLang)
        {
            setGameLang = true;
            setTsLang = true;
        }

        if (remaining.Count == 0)
        {
            if (setGameLang)
            {
                this.ResetGameLocale();
            }
            if (setTsLang)
            {
                this._targetCulture = null;
                this.SetTShockLocale(null);
            }
            return;
        }

        if (setGameLang)
        {
            if (!Utils.TryParseGameCulture(remaining[0], out var culture))
            {
                args.Player.SendErrorMessage($"Unrecognized culture {remaining[0]}");
                return;
            }
            LanguageManager.Instance.SetLanguage(culture);
            if (setTsLang)
            {
                this._targetCulture = Utils.CultureRedirect(culture.CultureInfo);
                this.SetTShockLocale(this._targetCulture);
            }
        }
        else
        {
            this.SetTShockLocale(this._targetCulture);
        }
    }

    private void SetTShockLocale(CultureInfo? culture)
    {
        var tscdir = this._tshockI18n
            .GetProperty("TranslationsDirectory", BindingFlags.NonPublic | BindingFlags.Static)!
            .GetGetMethod(true)!;
        this._tshockI18n.GetField("C")!.SetValue(null, new Catalog("TShockAPI",
            (string) tscdir.Invoke(null, new object[0])!, culture ?? (CultureInfo) this._tscinfo.Invoke(null, new object[0])!));
    }

    private void ResetGameLocale()
    {
        typeof(System.Globalization.CultureInfo).GetField("s_currentThreadUICulture", _bfany)?.SetValue(null, null);
        _ = Utils.TryParseGameCulture(System.Globalization.CultureInfo.CurrentUICulture.ToString(), out var result, true);
        Terraria.Localization.LanguageManager.Instance.SetLanguage(result!);
        CultureInfo.CurrentUICulture = Utils.CultureRedirect(CultureInfo.CurrentUICulture);
    }
}