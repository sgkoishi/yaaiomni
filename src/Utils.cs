using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
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
    /// This is a rough inverse of <see cref="TShockAPI.Commands.ParseParameters(string)"/>.
    /// </summary>
    public static string ToCommand(string specifier, string command, List<string> args)
    {
        return string.Join(" ", new List<string> { $"{specifier}{command}" }.Concat(args.Select(arg =>
        {
            var parg = arg.Replace("\\", "\\\\").Replace("\"", "\\\"");
            if (parg.Contains(' ') || parg.Contains('\\'))
            {
                return $"\"{parg}\"";
            }
            else
            {
                return parg;
            }
        })));
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
            foreach (var acc in TShockAPI.TShock.UserAccounts.GetUserAccounts())
            {
                yield return acc;
            }
            yield break;
        }
        if (pat.StartsWith("tsp:"))
        {
            pat = pat[4..];
            var exact = TShockAPI.TShock.Players.SingleOrDefault(p => p?.Name == pat)?.Account;
            if (exact != null)
            {
                yield return exact;
                yield break;
            }

            foreach (var acc in TShockAPI.TShock.Players.Where(p => p?.Account != null && p.Active && p.Account.Name.StartsWith(pat) == true).Select(p => p!.Account))
            {
                yield return acc;
            }

            foreach (var acc in TShockAPI.TShock.Players.Where(p => p?.Account != null && p.Active && p.Account.Name.Contains(pat) == true).Select(p => p!.Account))
            {
                yield return acc;
            }
            yield break;
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
            yield break;
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
            foreach (var acc in TShockAPI.TShock.UserAccounts.GetUserAccounts().Where(a => a.Name.StartsWith(pat)))
            {
                yield return acc;
            }
            foreach (var acc in TShockAPI.TShock.UserAccounts.GetUserAccounts().Where(a => a.Name.Contains(pat)))
            {
                yield return acc;
            }
            yield break;
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
            yield break;
        }
    }

    private static ConditionalWeakTable<TSPlayer, ReaderWriterLockSlim> _playerDataLocks = new ConditionalWeakTable<TSPlayer, ReaderWriterLockSlim>();

    public static T GetOrCreatePlayerAttachedData<T>(this TSPlayer player, string key) where T : new()
    {
        return player.GetOrCreatePlayerAttachedData(key, () => new T());
    }

    public static T GetOrCreatePlayerAttachedData<T>(this TSPlayer player, string key, Func<T> factory)
    {
        var l = _playerDataLocks.GetOrCreateValue(player);
        l.EnterUpgradeableReadLock();
        try
        {
            var value = player.GetData<T>(key);
            if (value is not T)
            {
                l.EnterWriteLock();
                try
                {
                    value = factory();
                    player.SetData(key, value);
                }
                finally
                {
                    l.ExitWriteLock();
                }
            }
            return value;
        }
        finally
        {
            l.ExitUpgradeableReadLock();
        }
    }

    public static void SetPlayerAttachedData<T>(this TSPlayer player, string key, T value)
    {
        var l = _playerDataLocks.GetOrCreateValue(player);
        l.EnterWriteLock();
        try
        {
            player.SetData(key, value);
        }
        finally
        {
            l.ExitWriteLock();
        }
    }
}
