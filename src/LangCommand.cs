using GetText;
using System.Globalization;
using System.Reflection;
using Terraria.Localization;
using TerrariaApi.Server;
using TShockAPI;

namespace Chireiden.TShock.Omni;

public partial class Plugin
{
    private CultureInfo? _targetCulture = null;
    private readonly Type _tshockI18n = Utils.TShockType("I18n");

    [Command("Admin.ManageLanguage", "chireiden.omni.setlang", "setlang")]
    private void Command_Lang(CommandArgs args)
    {
        var tscinfo = this._tshockI18n
            .GetProperty("TranslationCultureInfo", BindingFlags.NonPublic | BindingFlags.Static)!
            .GetGetMethod(true)!;

        if (args.Parameters.Count == 0)
        {
            args.Player.SendInfoMessage($"Current TShock Lang: {this._targetCulture ?? tscinfo.Invoke(null, new object[0])}");
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

        this._targetCulture = null;
        GameCulture? culture = null;
        if (remaining.Count != 0)
        {
            this._targetCulture = Utils.TryParseGameCulture(remaining[0], out culture)
                ? Utils.CultureRedirect(culture.CultureInfo)
                : Utils.CultureRedirect(CultureInfo.GetCultureInfo(remaining[0]));
        }

        if (setGameLang && culture != null)
        {
            LanguageManager.Instance.SetLanguage(culture);
        }

        if (setTsLang)
        {
            var tscdir = this._tshockI18n
                .GetProperty("TranslationsDirectory", BindingFlags.NonPublic | BindingFlags.Static)!
                .GetGetMethod(true)!;
            this._tshockI18n.GetField("C")!.SetValue(null, new Catalog("TShockAPI",
                (string) tscdir.Invoke(null, new object[0])!,
                this._targetCulture ?? (CultureInfo) tscinfo.Invoke(null, new object[0])!));
        }
    }
}