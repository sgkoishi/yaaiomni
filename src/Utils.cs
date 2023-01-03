using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Terraria.Localization;
using TShockAPI;
using static Terraria.Utils;
using TileCollection = ModFramework.ICollection<Terraria.ITile>;

namespace Chireiden.TShock.Omni;

public static class Utils
{
    internal static TileActionAttempt WithPermissionCheck(TileActionAttempt action, TSPlayer player)
    {
        return (x, y) => player.HasBuildPermission(x, y) && action(x, y);
    }

    public static bool TryParseGameCulture(string s, [NotNullWhen(returnValue: true)] out GameCulture? culture)
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

    public static CultureInfo CultureRedirect(CultureInfo cultureInfo)
        => cultureInfo.Name == "zh-Hans" ? new CultureInfo("zh-CN") : cultureInfo;

    public static Group? ParentGroup(Group? group, Func<Group, bool> predicate)
    {
        var hashset = new HashSet<string>();
        if (group == null || !predicate(group))
        {
            return null;
        }
        while (true)
        {
            if (!hashset.Add(group.Name))
            {
                return null;
            }

            var parent = group.Parent;
            if (parent == null || !predicate(parent))
            {
                return group;
            }
            group = parent;
        }
    }

    public static TileCollection CloneTileCollection(TileCollection existing, TileCollection newstorage)
    {
        for (var x = 0; x < existing.Width; x++)
        {
            for (var y = 0; y < existing.Height; y++)
            {
                newstorage[x, y] = existing[x, y];
            }
        }
        return newstorage;
    }
}
