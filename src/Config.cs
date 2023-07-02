namespace Chireiden.TShock.Omni;

/// <summary>
/// This is the config file for Omni.
/// </summary>
public class Config
{
    /// <summary>
    /// Weather to show the config file on load/reload.
    /// </summary>
    public Optional<bool> ShowConfig = Optional.Default(false);

    /// <summary>
    /// Weather to log all exceptions.
    /// </summary>
    public Optional<bool> LogFirstChance = Optional.Default(false);

    /// <summary>
    /// DateTime format for logging.
    /// </summary>
    public Optional<string> DateTimeFormat = Optional.Default("yyyy-MM-dd HH:mm:ss.fff");

    /// <summary>
    /// The wildcard of matching all players. Directly using "*" itself is not
    /// suggested as some commands might have special meaning for it.
    /// </summary>
    public Optional<List<string>> PlayerWildcardFormat = Optional.Default(new List<string> {
        "*all*"
    });

    /// <summary>
    /// The pattern of matching the server itself.
    /// </summary>
    public Optional<List<string>> ServerWildcardFormat = Optional.Default(new List<string> {
        "*server*",
        "*console*",
    });

    public Optional<List<string>> HideCommands = Optional.Default(new List<string> {
        DefinedConsts.Commands.Whynot,
        DefinedConsts.Commands.PvPStatus,
        DefinedConsts.Commands.TeamStatus,
        DefinedConsts.Commands.Admin.GarbageCollect,
        DefinedConsts.Commands.Admin.DebugStat,
        DefinedConsts.Commands.ResetCharacter,
        DefinedConsts.Commands.Ping,
        DefinedConsts.Commands.Chat,
        DefinedConsts.Commands.Echo,
        DefinedConsts.Commands.Admin.UpsCheck,
        DefinedConsts.Commands.Admin.ApplyDefaultPermission
    });

    public Optional<List<string>> StartupCommands = Optional.Default(new List<string>());

    public Optional<Dictionary<string, List<string>>> CommandRenames = Optional.Default(new Dictionary<string, List<string>>());

    public Optional<EnhancementsSettings> Enhancements = Optional.Default(new EnhancementsSettings());

    public Optional<LavaSettings> LavaHandler = Optional.Default(new LavaSettings(), true);

    public Optional<DebugPacketSettings> DebugPacket = Optional.Default(new DebugPacketSettings());

    public Optional<SoundnessSettings> Soundness = Optional.Default(new SoundnessSettings(), true);

    public Optional<PermissionSettings> Permission = Optional.Default(new PermissionSettings());

    public Optional<Modes> Mode = Optional.Default(new Modes());

    public Optional<MitigationSettings> Mitigation = Optional.Default(new MitigationSettings());

    public record class EnhancementsSettings
    {
        /// <summary>
        /// Remove unused client-side objects to save memory.
        /// </summary>
        public Optional<bool> TrimMemory = Optional.Default(true, true);

        /// <summary>
        /// Alternative command syntax implementation.
        /// Allow multiple commands in one line, quote inside text (e.g. te"x"t)
        /// <para>
        /// Note: this is not fully compatible with TShock's command syntax.
        /// </para>
        /// </summary>
        public Optional<bool> AlternativeCommandSyntax = Optional.Default(true);

        /// <summary>
        /// Override config file with CLI input (port, maxplayers)
        /// </summary>
        public Optional<bool> CLIoverConfig = Optional.Default(true, true);

        /// <summary>
        /// Disable vanilla version check.
        /// </summary>
        public Optional<bool> SyncVersion = Optional.Default(false);

        /// <summary>
        /// Fix the broken default language detect.
        /// <see href="https://github.com/Pryaxis/TShock/issues/2957" />
        /// </summary>
        public Optional<bool> DefaultLanguageDetect = Optional.Default(true, true);

        /// <summary>
        /// Action for TShock's update
        /// </summary>
        public Optional<UpdateOptions> SuppressUpdate = Optional.Default(UpdateOptions.Silent);

        /// <summary>
        /// Socket Provider
        /// </summary>
        public Optional<SocketType> Socket = Optional.Default(SocketType.AnotherAsyncSocketAsFallback, true);

        public Optional<NameCollisionAction> NameCollision = Optional.Default(NameCollisionAction.Unhandled, true);

        public Optional<TileProviderOptions> TileProvider = Optional.Default(TileProviderOptions.AsIs, true);

        /// <summary>
        /// Support regex (`namea:player.*`) and IP mask (`ipa:1.1.0.0/16`).
        /// </summary>
        public Optional<bool> BanPattern = Optional.Default(true, true);

        public enum UpdateOptions
        {
            Silent,
            Disabled,
            AsIs,
        }

        /// <summary>
        /// We found 'memory leak', from the memory dump it seems that the async networking is using much more memory than expected.
        /// <code>
        /// System.Threading.ThreadPool.s_workQueue
        /// -> System.Net.Sockets.SocketAsyncContext+BufferMemorySendOperation
        ///   -> System.Action&lt;System.Int32, System.Byte[], System.Int32, System.Net.Sockets.SocketFlags, System.Net.Sockets.SocketError&gt;
        ///     -> System.Net.Sockets.Socket.AwaitableSocketAsyncEventArgs
        /// -> System.Threading.QueueUserWorkItemCallbackDefaultContext
        ///   -> System.Net.Sockets.SocketAsyncContext+BufferMemorySendOperation
        ///     -> System.Action&lt;System.Int32, System.Byte[], System.Int32, System.Net.Sockets.SocketFlags, System.Net.Sockets.SocketError&gt;
        ///       -> System.Net.Sockets.Socket.AwaitableSocketAsyncEventArgs
        /// </code>
        /// This 'memory leak' is now confirmed to be related to <seealso cref="Chireiden.TShock.Omni.Config.MitigationSettings.InventorySlotPE"/>.
        /// </summary>
        public enum SocketType
        {
            Vanilla,
            TShock,
            AsIs,
            Unset,
            HackyBlocked,
            HackyAsync,
            AnotherAsyncSocket,
            AnotherAsyncSocketAsFallback
        }

        public enum NameCollisionAction
        {
            /// <summary>
            /// Kick the first player
            /// </summary>
            First,
            /// <summary>
            /// Kick the second player
            /// </summary>
            Second,
            /// <summary>
            /// Kick both players
            /// </summary>
            Both,
            /// <summary>
            /// Kick neither player
            /// </summary>
            None,
            /// <summary>
            /// Kick whoever does not using a known ip and not logged in, fallback to <see cref="Second"/>
            /// </summary>
            Known,
            /// <summary>
            /// Do nothing
            /// </summary>
            Unhandled,
        }

        public enum TileProviderOptions
        {
            AsIs,
            CheckedTypedCollection,
            CheckedGenericCollection,
        }
    }

    public record class LavaSettings
    {
        public Optional<bool> Enabled = Optional.Default(false);
        public Optional<bool> AllowHellstone = Optional.Default(false);
        public Optional<bool> AllowCrispyHoneyBlock = Optional.Default(false);
        public Optional<bool> AllowHellbat = Optional.Default(false);
        public Optional<bool> AllowLavaSlime = Optional.Default(false);
        public Optional<bool> AllowLavabat = Optional.Default(false);
    }

    public record class DebugPacketSettings
    {
        public Optional<PacketFilter> In = Optional.Default(new PacketFilter(false), true);
        public Optional<PacketFilter> Out = Optional.Default(new PacketFilter(false), true);
        public Optional<PacketFilter> BytesOut = Optional.Default(new PacketFilter(false), true);
        public Optional<CatchedException> ShowCatchedException = Optional.Default(CatchedException.Uncommon);

        public enum CatchedException
        {
            None,
            Uncommon,
            All,
        }

        public unsafe struct PacketFilter : IEquatable<PacketFilter>
        {
            public static readonly byte MaxPacket = (byte) Enum.GetValues(typeof(PacketTypes)).Cast<int>().Max();
            private fixed bool _matches[byte.MaxValue];

            public PacketFilter(bool accept)
            {
                for (var i = 0; i < byte.MaxValue; i++)
                {
                    this._matches[i] = accept;
                }
            }

            public PacketFilter(params byte[] accept)
            {
                foreach (var value in accept)
                {
                    this._matches[value] = true;
                }
            }

            public readonly bool Handle(byte type)
            {
                return this._matches[type];
            }

            public readonly bool Handle(int type)
            {
                return this.Handle((byte) type);
            }

            public readonly bool Handle(PacketTypes type)
            {
                return this.Handle((byte) type);
            }

            public readonly bool Equals(PacketFilter other)
            {
                var eq = true;
                for (var i = 0; i < byte.MaxValue; i++)
                {
                    eq &= this._matches[i] == other._matches[i];
                }
                return eq;
            }

            public override readonly bool Equals(object? obj)
            {
                return obj is PacketFilter pf && this.Equals(pf);
            }

            public override readonly int GetHashCode()
            {
                var h = 0;
                for (var i = 0; i < byte.MaxValue; i++)
                {
                    h ^= Convert.ToInt32(this._matches[i]) << (i % 32);
                }
                return h;
            }

            public static bool operator ==(PacketFilter left, PacketFilter right) => left.Equals(right);

            public static bool operator !=(PacketFilter left, PacketFilter right) => !(left == right);
        }
    }

    public record class SoundnessSettings
    {
        /// <summary>
        /// Permission restrict server-side tile modification projectiles like liquid bombs &amp; rockets, dirt bombs.
        /// </summary>
        public Optional<bool> ProjectileKillMapEditRestriction = Optional.Default(true);

        /// <summary>
        /// Restrict quick stack to have build permission.
        /// </summary>
        public Optional<bool> QuickStackRestriction = Optional.Default(true);

        /// <summary>
        /// Restrict sign edit to have build permission.
        /// </summary>
        public Optional<bool> SignEditRestriction = Optional.Default(true);
    }

    public record class PermissionSettings
    {
        public Optional<PermissionLogSettings> Log = Optional.Default(new PermissionLogSettings());
        public Optional<RestrictSettings> Restrict = Optional.Default(new RestrictSettings(), true);
        public Optional<PresetSettings> Preset = Optional.Default(new PresetSettings());

        public record class PermissionLogSettings
        {
            public Optional<bool> Enabled = Optional.Default(true);
            public Optional<int> LogCount = Optional.Default(50);
            public Optional<bool> LogDuplicate = Optional.Default(false, true);
            public Optional<double> LogDistinctTime = Optional.Default(1.0, true);
            public Optional<bool> LogStackTrace = Optional.Default(false, true);
        }

        public record class RestrictSettings
        {
            public Optional<bool> Enabled = Optional.Default(false);
            public Optional<bool> ToggleTeam = Optional.Default(true);
            public Optional<bool> TogglePvP = Optional.Default(true);
            public Optional<bool> SyncLoadout = Optional.Default(true);
            public Optional<bool> SummonBoss = Optional.Default(true);
        }

        public record class PresetSettings
        {
            public Optional<bool> Enabled = Optional.Default(true);
            public Optional<bool> AlwaysApply = Optional.Default(false, true);
            public Optional<bool> DebugForAdminOnly = Optional.Default(false);
            public Optional<bool> AllowRestricted = Optional.Default(true, true);
        }
    }

    public record class Modes
    {
        public Optional<BuildingMode> Building = Optional.Default(new BuildingMode(), true);
        public Optional<PvPMode> PvP = Optional.Default(new PvPMode(), true);
        public Optional<VanillaMode> Vanilla = Optional.Default(new VanillaMode());

        public record class BuildingMode
        {
            public Optional<bool> Enabled = Optional.Default(false);
        }

        public record class PvPMode
        {
            public Optional<bool> Enabled = Optional.Default(false);
        }

        public record class VanillaMode
        {
            public Optional<bool> Enabled = Optional.Default(false);
            public Optional<List<string>> Permissions = Optional.Default(new List<string> {
                TShockAPI.Permissions.canregister,
                TShockAPI.Permissions.canlogin,
                TShockAPI.Permissions.canlogout,
                TShockAPI.Permissions.canchangepassword,
                TShockAPI.Permissions.hurttownnpc,
                TShockAPI.Permissions.spawnpets,
                TShockAPI.Permissions.summonboss,
                TShockAPI.Permissions.startinvasion,
                TShockAPI.Permissions.startdd2,
                TShockAPI.Permissions.home,
                TShockAPI.Permissions.spawn,
                TShockAPI.Permissions.rod,
                TShockAPI.Permissions.wormhole,
                TShockAPI.Permissions.pylon,
                TShockAPI.Permissions.tppotion,
                TShockAPI.Permissions.magicconch,
                TShockAPI.Permissions.demonconch,
                TShockAPI.Permissions.editspawn,
                TShockAPI.Permissions.usesundial,
                TShockAPI.Permissions.movenpc,
                TShockAPI.Permissions.canbuild,
                TShockAPI.Permissions.canpaint,
                TShockAPI.Permissions.toggleparty,
                TShockAPI.Permissions.whisper,
                TShockAPI.Permissions.canpartychat,
                TShockAPI.Permissions.cantalkinthird,
                TShockAPI.Permissions.canchat,
                TShockAPI.Permissions.synclocalarea,
                TShockAPI.Permissions.sendemoji,
                DefinedConsts.Permission.TogglePvP,
                DefinedConsts.Permission.ToggleTeam,
                DefinedConsts.Permission.SyncLoadout,
                DefinedConsts.Permission.Ping
            });
            public Optional<bool> AllowJourney = Optional.Default(false);
            public Optional<bool> IgnoreAntiCheat = Optional.Default(false);
            public Optional<VanillaAntiCheat> AntiCheat = Optional.Default(new VanillaAntiCheat(), true);

            public record class VanillaAntiCheat
            {
                public Optional<bool> Enabled = Optional.Default(false);
            }
        }
    }

    public record class MitigationSettings
    {
        public Optional<bool> Enabled = Optional.Default(true);

        /// <summary>
        /// <para>
        /// Mobile (PE) client keep (likely per frame) send PlayerSlot packet
        /// to the server if any item exists in non-active loadout.
        /// </para>
        /// <para>Cause lag and high memory usage.</para>
        /// <para>
        /// This will silently proceed the packet without boardcasting it, and
        /// stop future unnecessary sync.
        /// </para>
        /// Tracking: <see href="https://forums.terraria.org/index.php?threads/network-broadcast-storm.117270/"/>
        /// </summary>
        public Optional<bool> InventorySlotPE = Optional.Default(true, true);

        /// <summary>
        /// <para>
        /// Mobile (PE) client can use healing potion (etc.) without getting
        /// or being restricted by PotionSickness.
        /// </para>
        /// <para>Cause imbalance.</para>
        /// <para>
        /// This will silently revert the attempt of healing.
        /// Item is still consumed as punishment.
        /// </para>
        /// Tracking: <see href="https://forums.terraria.org/index.php?threads/almost-invincible-by-healing-with-potions-but-without-cooldown.117269/"/>
        /// </summary>
        public Optional<bool> PotionSicknessPE = Optional.Default(true, true);

        /// <summary>
        /// <para>
        /// Similar to <seealso cref="PotionSicknessPE"/>, but generic for
        /// all items.
        /// </para>
        /// <para>Cause imbalance.</para>
        /// <para>
        /// This will silently revert the attempt of using the item.
        /// Might cause player slightly desync when they try to do so.
        /// </para>
        /// Tracking: <see href="https://forums.terraria.org/index.php?threads/almost-invincible-by-healing-with-potions-but-without-cooldown.117269/"/>
        /// </summary>
        public Optional<bool> SwapWhileUsePE = Optional.Default(true, true);

        /// <summary>
        /// <para>
        /// Chat spam rate limit. This restriction also applies to commands.
        /// Each item is a pair of rate and maximum.
        /// </para>
        /// <para>Higher rate and lower maximum means more strict.</para>
        /// <para>
        /// The default limit:
        ///   3 messages per 5 seconds,
        ///   5 messages per 20 seconds
        /// </para>
        /// </summary>
        public Optional<List<LimiterConfig>> ChatSpamRestrict = Optional.Default(new List<LimiterConfig> {
            new LimiterConfig { RateLimit = 1.6, Maximum = 5 },
            new LimiterConfig { RateLimit = 4, Maximum = 20 }
        });

        /// <summary>
        /// <para>
        /// Restrict the rate of sending <seealso cref="PacketTypes.NpcUpdateBuff"/>.
        /// In some cases, the client will send <seealso cref="PacketTypes.NpcAddBuff"/> frequently,
        /// and the server will boardcast in O(n^2) and cause network storm.
        /// </para>
        /// <para>Likely caused by shimmer.</para>
        /// <para>
        /// This will replace the logic of these two packets and only boardcast at time interval.
        /// Use with caution.
        /// </para>
        /// </summary>
        public Optional<bool> NpcUpdateBuffRateLimit = Optional.Default(false, true);

        /// <summary>
        /// <para>
        /// Some terminals does not support Operating System Commands (OSCs) for setting the title.
        /// This will cause title being interpreted as stdio.
        /// </para>
        /// <para>
        /// linux should support OSC commands according to the implementation of <seealso cref="Console.Title"/>,
        /// but some doesn't.
        /// <see href="https://source.dot.net/#System.Console/System/TerminalFormatStrings.cs,e0a3bdd93a9caf05,references" />
        /// </para>
        /// <para>Cause spam in console.</para>
        /// <para>
        /// This will prevent the title from being set if TERM has no xterm.
        /// </para>
        /// </summary>
        public Optional<bool> SuppressTitle = Optional.Default(true, true);

        /// <summary>
        /// <para>
        /// Some script kiddies spam connection requests to the server and occupy the connection pool.
        /// </para>
        /// <para>Cause player unable to connect.</para>
        /// <para>
        /// This will silently drop the connection request that exceeds the limit.
        /// Does not apply to local address.
        /// </para>
        /// <para>
        /// The default limit:
        ///   1.6 connections per 5 seconds,
        ///   4 connections per 60 seconds
        /// </para>
        /// </summary>
        public Optional<List<LimiterConfig>> ConnectionLimit = Optional.Default(new List<LimiterConfig> {
            new LimiterConfig { RateLimit = 3, Maximum = 5 },
            new LimiterConfig { RateLimit = 15, Maximum = 60 },
        });

        /// <summary>
        /// <para>
        /// Some script kiddies spam connection requests to the server and occupy the connection pool.
        /// </para>
        /// <para>Cause player unable to connect.</para>
        /// <para>
        /// This will disconnect the client if they are in the state for too long.
        /// Also apply to local address.
        /// </para>
        /// <para>
        /// The default limit:
        ///   Socket created: 1 second
        ///   <seealso cref="PacketTypes.ConnectRequest"> received: +3 seconds
        /// </para>
        /// </summary>
        public Optional<Dictionary<int, double>> ConnectionStateTimeout = Optional.Default(new Dictionary<int, double> {
            { 0, 1 },
            { 1, 4 },
        }, true);

        /// <summary>
        /// <para>
        /// Disabled players are restricted from most actions including being hurt.
        /// </para>
        /// <para>Cause imbalance.</para>
        /// <para>
        /// This will allow disabled players to be hurt.
        /// </para>
        /// <see href="https://github.com/Pryaxis/TShock/issues/1151" />
        /// </summary>
        public Optional<DisabledDamageAction> DisabledDamageHandler = Optional.Default(DisabledDamageAction.Hurt, true);

        /// <summary>
        /// <para>
        /// In expert mode enemies can pick up coins.
        /// Each client will attempts to pick up coins, causing the NPC picking up multiple times,
        /// and grows exponentially as the iteration goes.
        /// </para>
        /// <para>Cause imbalance.</para>
        /// <para>
        /// This will try to change the behavior of the coin pickup.
        /// </para>
        /// <see href="https://github.com/Pryaxis/TShock/issues/2004"/>
        /// </summary>
        public Optional<ExpertCoinHandler> ExpertExtraCoin = Optional.Default(ExpertCoinHandler.ServerSide, true);

        /// <summary>
        /// <para>
        /// The legacy HttpServer.dll from .NET Framework 2.0 era is not responding correctly if
        /// the request is too long.
        /// </para>
        /// <para>Cause broken REST endpoint.</para>
        /// <para>
        /// This will try to remove the Connection header from the request.
        /// </para>
        /// <see href="https://github.com/Pryaxis/TShock/issues/2923"/>
        /// </summary>
        public Optional<bool> KeepRestAlive = Optional.Default(true, true);

        /// <summary>
        /// <para>
        /// Terraria will translate chat commands into command id. TShock
        /// translate them back to keep the command working.
        /// However, when the server and the client have different locale,
        /// a enUS player send `/help` will be sent as `CommandId: Help`
        /// and a deDE server will translate it back to `/hilfe`, thus the
        /// command is broken.
        /// </para>
        /// <para>Cause some commands broken.</para>
        /// <para>
        /// This will try to change the translate target to enUS, so that
        /// the command will be translated back to `/help`. A deDE player
        /// may run `/help` (CommandId: Say, Content: /help) or
        /// `/hilfe` (CommandId: Help), and both works.
        /// </para>
        /// <see href="https://github.com/Pryaxis/TShock/issues/2914"/>
        /// </summary>
        public Optional<bool> UseEnglishCommand = Optional.Default(true, true);

        /// <summary>
        /// <para>
        /// TShock update legacy config { "key1": "value1", "key2": "value2" } to
        /// the new format { "Settings": { "key1": "value1", "key2": "value2" } }.
        /// If the config is partially updated, { "key1": "value1", "Settings": { 
        /// "key2": "value2" } }, the key1 is discarded.
        /// </para>
        /// <para>Cause some config options not being applied.</para>
        /// <para>
        /// This will allow those partially updated config.
        /// </para>
        /// </summary>
        public Optional<PartialConfigAction> AcceptPartialUpdatedConfig = Optional.Default(PartialConfigAction.Replace, true);

        public enum DisabledDamageAction
        {
            AsIs,
            Hurt,
            Ghost
        }

        public enum ExpertCoinHandler
        {
            /// <summary>
            /// Disable the picked up coin value. Some coins may vanish.
            /// <para>
            DisableValue,
            /// <summary>
            /// Server side coin pickup.
            /// <para>
            ServerSide,
            /// <summary>
            /// Untouched like vanilla.
            /// <para>
            AsIs,
        }

        public enum PartialConfigAction
        {
            Ignore,
            Replace,
            // Merge
        }
    }

    public record class LimiterConfig
    {
        public double RateLimit { get; set; }
        public double Maximum { get; set; }

        public static explicit operator Limiter(LimiterConfig config)
        {
            return new Limiter
            {
                Config = config
            };
        }
    }
}

public class Limiter
{
    public required Config.LimiterConfig Config { get; set; }
    public double Counter { get; set; }
    public bool Allowed
    {
        get
        {
            var time = new TimeSpan(DateTime.Now.Ticks).TotalSeconds;
            var tat = Math.Max(this.Counter, time) + this.Config.RateLimit;
            if (tat > time + this.Config.Maximum)
            {
                return false;
            }
            this.Counter = tat;
            return true;
        }
    }
}

public abstract class Optional
{
    public abstract bool IsHiddenValue();
    public abstract object? ObjectValue { get; set; }
    public static Optional<T> Default<T>(T value, bool hide = false)
    {
        return new Optional<T>(value, hide);
    }
}

public class Optional<T> : Optional, IEquatable<Optional<T>>
{
    public bool IsDefault { private set; get; }
    public bool HideWhenDefault { private set; get; }
    private readonly T _defaultValue;
    private T? _value;
    public T Value
    {
        get => this.IsDefault ? this._defaultValue : this._value!;
        set
        {
            if (EqualityComparer<T>.Default.Equals(value, this._defaultValue))
            {
                this.IsDefault = true;
            }
            else
            {
                this.IsDefault = false;
                this._value = value;
            }
        }
    }

    public override bool IsHiddenValue()
    {
        return this.IsDefault && this.HideWhenDefault;
    }

    public bool Equals(Optional<T>? other)
    {
        return this.IsDefault == other?.IsDefault
            && EqualityComparer<T>.Default.Equals(this._defaultValue, other._defaultValue)
            && EqualityComparer<T>.Default.Equals(this._value, other._value);
    }

    public override object? ObjectValue
    {
        get => this.Value;
        set
        {
            if (value is T t)
            {
                this.Value = t;
            }
        }
    }

    public Optional(T value, bool hide = false)
    {
        this.IsDefault = true;
        this._defaultValue = value;
        this.HideWhenDefault = hide;
    }

    public static implicit operator T(Optional<T> self) => self.Value;

    public override bool Equals(object? obj)
    {
        return obj is Optional<T> ot && this.Equals(ot);
    }

    public override int GetHashCode()
    {
        if (this._defaultValue is null)
        {
            return 0;
        }
        var v = this._value is null
            ? 0
            : EqualityComparer<T>.Default.GetHashCode(this._value);
        return EqualityComparer<T>.Default.GetHashCode(this._defaultValue) ^ v;
    }
}