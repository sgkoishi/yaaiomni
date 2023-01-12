using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Terraria.Localization;
using TShockAPI;
using static Terraria.Utils;
using TileCollection = ModFramework.ICollection<Terraria.ITile>;

namespace Chireiden.TShock.Omni;

public static class Utils
{
    internal static TileActionAttempt WithPermissionCheck(TileActionAttempt action, TSPlayer? player)
    {
        return (x, y) => (player?.HasBuildPermission(x, y) ?? false) && action(x, y);
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

    internal static void TryRenameCommand(Command command, Dictionary<string, List<string>> newnames)
    {
        var method = command.CommandDelegate.Method;
        var sig = $"{method.DeclaringType?.FullName}.{method.Name}";
        if (newnames.TryGetValue(sig, out var names))
        {
            command.Names.Clear();
            command.Names.AddRange(names);
        }
    }

#pragma warning disable CS1574 // ParseParameters could not be resolved because it is private
    /// <summary>
    /// Converts a list of arguments back to a command.
    /// This is a rough inverse of <see cref="TShockAPI.Commands.ParseParameters(string)"/>.
    /// </summary>
#pragma warning restore CS1574
    public static string ToCommand(string specifier, string command, List<string> args)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append(specifier).Append(command).Append(' ');
        foreach (var arg in args)
        {
            var parg = arg.Replace("\\", "\\\\").Replace("\"", "\\\"");
            if (parg.Contains(' '))
            {
                sb.Append('"').Append(parg).Append('"');
            }
            else
            {
                sb.Append(parg);
            }
        }
        return sb.ToString();
    }

    public static IEnumerable<TSPlayer> ActivePlayers => TShockAPI.TShock.Players.Where(p => p != null && p.Active);
}
