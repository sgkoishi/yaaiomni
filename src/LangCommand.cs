using GetText;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using Terraria.Localization;
using TerrariaApi.Server;
using TShockAPI;

namespace Chireiden.TShock.Omni;

public partial class Plugin : TerrariaPlugin
{
    private CultureInfo? _targetCulture = null;
    private Type _tshockI18n = typeof(TShockAPI.TShock).Module.GetTypes().Single(t => t.Name == "I18n")!;
    private void LangCommand(CommandArgs args)
    {
        static CultureInfo Redirect(CultureInfo cultureInfo)
            => cultureInfo.Name == "zh-Hans" ? new CultureInfo("zh-CN") : cultureInfo;

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
            if (this.TryParseGameCulture(remaining[0], out culture))
            {
                this._targetCulture = Redirect(culture.CultureInfo);
            }
            else
            {
                this._targetCulture = Redirect(CultureInfo.GetCultureInfo(remaining[0]));
            }
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
            _tshockI18n.GetField("C")!.SetValue(null, new Catalog("TShockAPI",
                (string) tscdir.Invoke(null, new object[0])!,
                this._targetCulture ?? (CultureInfo) tscinfo.Invoke(null, new object[0])!));
        }
    }

    bool TryParseGameCulture(string s, [NotNullWhen(returnValue: true)] out GameCulture? culture)
    {
        if (int.TryParse(s, out var number))
        {
            if (GameCulture._legacyCultures.TryGetValue(number, out culture))
            {
                return true;
            }
        }

        culture = GameCulture._legacyCultures.Values.SingleOrDefault(c => c.Name == s);
        if (culture != null)
        {
            return true;
        }

        culture = GameCulture._legacyCultures.Values.SingleOrDefault(c => c.CultureInfo.NativeName == s);
        if (culture != null)
        {
            return true;
        }

        culture = GameCulture._legacyCultures.Values.SingleOrDefault(c => c.CultureInfo.NativeName.Contains(s));
        if (culture != null)
        {
            return true;
        }

        return false;
    }
}
