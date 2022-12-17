using GetText;
using System.Globalization;
using System.Reflection;
using Terraria.Localization;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace Chireiden.TShock.Omni;

public partial class Plugin : TerrariaPlugin
{
    private CultureInfo? _targetCulture = null;
    private Type _tshockI18n = typeof(TShockAPI.TShock).Module.GetTypes().Single(t => t.Name == "I18n")!;
    private void LangCommand(CommandArgs args)
    {
        static CultureInfo Redirect(CultureInfo cultureInfo)
            => cultureInfo.Name == "zh-Hans" ? new CultureInfo("zh-CN") : cultureInfo;

        var cultureinfo = this._tshockI18n
            .GetProperty("TranslationCultureInfo", BindingFlags.NonPublic | BindingFlags.Static)!
            .GetGetMethod(true)!;

        if (args.Parameters.Count == 0)
        {
            args.Player.SendInfoMessage($"Current Lang: {this._targetCulture ?? cultureinfo.Invoke(null, new object[0])}");
            return;
        }

        this._targetCulture = null;
        if (int.TryParse(args.Parameters[0], out var number))
        {
            if (GameCulture._legacyCultures.TryGetValue(number, out var culture))
            {
                this._targetCulture = Redirect(culture.CultureInfo);
            }
        }
        if (this._targetCulture == null)
        {
            var culture = GameCulture._legacyCultures.Values.SingleOrDefault(c => c.Name == args.Parameters[0]);
            if (culture != null)
            {
                this._targetCulture = Redirect(culture.CultureInfo);
            }
        }

        _tshockI18n.GetField("C").SetValue(null, new Catalog("TShockAPI",
            Path.Combine(AppContext.BaseDirectory, "i18n"), 
            this._targetCulture ?? (CultureInfo)cultureinfo.Invoke(null, new object[0])));
    }
}
