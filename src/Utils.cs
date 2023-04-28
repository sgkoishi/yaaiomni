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
        return culture != null;
    }

    public static CultureInfo CultureRedirect(CultureInfo cultureInfo)
    {
        return cultureInfo.Name == "zh-Hans" ? new CultureInfo("zh-CN") : cultureInfo;
    }

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

    /// <summary>
    /// Converts a list of arguments back to a command.
    /// This is a rough inverse of <seealso cref="TShockAPI.Commands.ParseParameters(string)"/>.
    /// Will not add specifier for you.
    /// </summary>
    public static string ToCommand(string command, List<string> args)
    {
        // Theoretically there should be no space to trim
        // but REST's rawcmd assume everything as command
        // and the alt syntax parse and keep the first character
        return string.Join(" ", new List<string> { $"{command}" }.Concat(args.Select(arg =>
        {
            var parg = arg.Replace("\\", "\\\\").Replace("\"", "\\\"");
            return parg.Contains(' ') || parg.Contains('\\') ? $"\"{parg}\"" : parg;
        }))).Trim();
    }

    /// <summary>
    /// Parse a string into a list of commands.
    /// Supports <c>command [args] [&amp;&amp; command [args]] ..</c> and
    /// <c>command [args] [; command [args]] ..</c>.
    /// Similar but not fully compatible with the syntax of <seealso cref="TShockAPI.Commands.ParseParameters(string)"/>.
    /// </summary>
    public static List<Plugin.ParsedCommand> ParseCommands(string input)
    {
        var result = new List<Plugin.ParsedCommand>();
        var currentIndex = 0;
        var inQuote = false;
        var currentCommand = new Plugin.ParsedCommand();
        while (currentIndex < input.Length)
        {
            switch (input[currentIndex])
            {
                case '\"':
                    inQuote = !inQuote;
                    break;
                case '\\':
                    if (currentIndex + 1 < input.Length)
                    {
                        currentIndex++;
                    }
                    currentCommand.AppendChar(input[currentIndex]);
                    break;
                case '&' when !inQuote && currentIndex + 1 < input.Length && input[currentIndex + 1] == '&':
                case ';' when !inQuote:
                    currentCommand.EndSegment();
                    currentCommand.ContinueOnError = input[currentIndex] == ';';
                    result.Add(currentCommand);
                    currentCommand = new Plugin.ParsedCommand();
                    currentIndex += currentCommand.ContinueOnError ? 0 : 1;
                    while (currentIndex + 1 < input.Length && char.IsWhiteSpace(input[currentIndex + 1]))
                    {
                        currentIndex++;
                    }
                    break;
                case char c when !inQuote && char.IsWhiteSpace(c):
                    currentCommand.EndSegment();
                    while (currentIndex + 1 < input.Length && char.IsWhiteSpace(input[currentIndex + 1]))
                    {
                        currentIndex++;
                    }
                    break;
                case char c:
                    currentCommand.AppendChar(c);
                    break;
            }
            currentIndex++;
        }
        currentCommand.EndSegment();
        if (!currentCommand.IsEmpty)
        {
            result.Add(currentCommand);
        }
        return result;
    }

    public static IEnumerable<TSPlayer> ActivePlayers => TShockAPI.TShock.Players.Where(p => p?.Active == true);

    public static IEnumerable<TShockAPI.DB.UserAccount> SearchUserAccounts(string? pat)
    {
        if (pat == null)
        {
            yield break;
        }
        if (pat == "*")
        {
            foreach (var acc in Utils.ActivePlayers.Select(p => p.Account).Where(a => a != null))
            {
                yield return acc;
            }
        }
        else if (pat.StartsWith("tsp:"))
        {
            pat = pat[4..];
            var exact = TShockAPI.TShock.Players.SingleOrDefault(p => p?.Name == pat)?.Account;
            if (exact != null)
            {
                yield return exact;
                yield break;
            }

            foreach (var acc in TShockAPI.TShock.Players
                .Where(p => p.Active && p?.Account.Name.Contains(pat) == true)
                .OrderBy(p => p.Account.Name.StartsWith(pat) ? 0 : 1)
                .Select(p => p.Account))
            {
                yield return acc;
            }
        }
        else if (pat.StartsWith("tsi:"))
        {
            if (int.TryParse(pat[4..], out var id))
            {
                var exact = TShockAPI.TShock.Players.SingleOrDefault(p => p?.Index == id)?.Account;
                if (exact != null)
                {
                    yield return exact;
                    yield break;
                }
            }
        }
        else if (pat.StartsWith("usr:"))
        {
            pat = pat[4..];
            var exact = TShockAPI.TShock.UserAccounts.GetUserAccountByName(pat);
            if (exact != null)
            {
                yield return exact;
                yield break;
            }

            foreach (var acc in TShockAPI.TShock.UserAccounts.GetUserAccounts()
                .Where(p => p?.Name.Contains(pat) == true)
                .OrderBy(p => p.Name.StartsWith(pat) ? 0 : 1))
            {
                yield return acc;
            }
        }
        else if (pat.StartsWith("usi:"))
        {
            if (int.TryParse(pat[4..], out var id))
            {
                var exact = TShockAPI.TShock.UserAccounts.GetUserAccountByID(id);
                if (exact != null)
                {
                    yield return exact;
                }
            }
        }
    }

    internal static bool PublicIPv4Address(System.Net.IPAddress address)
    {
        if (address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
        {
            return false;
        }

        return address.GetAddressBytes() switch
        {
            [10, _, _, _] => true,
            [127, _, _, _] => true,
            [192, 168, _, _] => true,
            [172, >= 16 and < 32, _, _] => true,
            [169, 254, _, _] => true,
            _ => false
        };
    }

    internal static Type TShockType(string name)
    {
        foreach (var type in typeof(TShockAPI.TShock).Module.GetTypes())
        {
            if (type.Name == name)
            {
                return type;
            }
        }
        throw new TypeLoadException($"Could not find type {name} in TShock");
    }
}
