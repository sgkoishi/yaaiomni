using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Chireiden.TShock.Omni;

/// <summary>
/// This is the config file for Omni.
/// </summary>
public class Config
{
    /// <summary>
    /// Weather to show the config file on load/reload.
    /// </summary>
    public bool ShowConfig = false;

    /// <summary>
    /// Weather to log all exceptions.
    /// </summary>
    public bool LogFirstChance = false;

    /// <summary>
    /// DateTime format for logging.
    /// </summary>
    public string DateTimeFormat = "yyyy-MM-dd HH:mm:ss.fff";

    /// <summary>
    /// The wildcard of matching all players. Directly using "*" itself is not
    /// suggested as some commands might have special meaning for it.
    /// </summary>
    public List<string> PlayerWildcardFormat = new List<string> {
        "*all*"
    };

    /// <summary>
    /// The pattern of matching the server itself.
    /// </summary>
    public List<string> ServerWildcardFormat = new List<string> {
        "*server*",
        "*console*",
    };

    public List<string> HideCommands = new List<string> {
        Plugin.PluginConsts.Commands.Whynot,
        LegacyConsts.Commands.SetPvp,
        LegacyConsts.Commands.SetTeam,
        LegacyConsts.Commands.TriggerGarbageCollection,
        LegacyConsts.Commands.DebugStat,
        LegacyConsts.Commands.ResetCharacter,
        LegacyConsts.Commands.Ping,
        LegacyConsts.Commands.Chat,
        LegacyConsts.Commands.Echo,
    };

    public List<string> StartupCommands = new List<string>();

    public Dictionary<string, List<string>> CommandRenames = new();

    public EnhancementsSettings Enhancements = new();

    public LavaSettings LavaHandler = new();

    public DebugPacketSettings DebugPacket = new();

    public SoundnessSettings Soundness = new();

    public PermissionSettings Permission = new();

    public Modes Mode = new();

    public MitigationSettings Mitigation = new();

    public class EnhancementsSettings
    {
        /// <summary>
        /// Remove unused client-side objects to save memory.
        /// </summary>
        public bool TrimMemory = true;

        /// <summary>
        /// Alternative command syntax implementation.
        /// Allow multiple commands in one line, quote inside text (e.g. te"x"t)
        /// <para>
        /// Note: this is not fully compatible with TShock's command syntax.
        /// </para>
        /// </summary>
        public bool AlternativeCommandSyntax = true;

        /// <summary>
        /// Override config file with CLI input (port, maxplayers)
        /// </summary>
        public bool CLIoverConfig = true;

        /// <summary>
        /// Disable vanilla version check.
        /// </summary>
        public bool SyncVersion = true;

        /// <summary>
        /// Action for TShock's update
        /// </summary>
        public UpdateOptions SuppressUpdate = UpdateOptions.Preset;

        /// <summary>
        /// Socket Provider
        /// </summary>
        public SocketType Socket = SocketType.Preset;

        public NameCollisionAction NameCollision = NameCollisionAction.Preset;

        public TileProviderOptions TileProvider = TileProviderOptions.Preset;

        /// <summary>
        /// Support regex (`namea:player.*`) and IP mask (`ipa:1.1.0.0/16`).
        /// </summary>
        public bool BanPattern = true;

        [JsonConverter(typeof(StringEnumConverter))]
        public enum UpdateOptions
        {
            Silent,
            Disabled,
            AsIs,
            Preset,
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
        [JsonConverter(typeof(StringEnumConverter))]
        public enum SocketType
        {
            Vanilla,
            TShock,
            AsIs,
            Unset,
            HackyBlocked,
            HackyAsync,
            AnotherAsyncSocket,
            Preset
        }

        [JsonConverter(typeof(StringEnumConverter))]
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
            Preset,
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum TileProviderOptions
        {
            AsIs,
            CheckedTypedCollection,
            CheckedGenericCollection,
            Preset,
        }
    }

    public class LavaSettings
    {
        public bool Enabled = false;
        public bool AllowHellstone = false;
        public bool AllowCrispyHoneyBlock = false;
        public bool AllowHellbat = false;
        public bool AllowLavaSlime = false;
        public bool AllowLavabat = false;
    }

    public class DebugPacketSettings
    {
        public bool In = false;
        public bool Out = false;
        public bool BytesOut = false;
        public CatchedException ShowCatchedException = CatchedException.Preset;

        [JsonConverter(typeof(StringEnumConverter))]
        public enum CatchedException
        {
            None = 1,
            Uncommon,
            All,
            Preset = 0
        }
    }

    public class SoundnessSettings
    {
        /// <summary>
        /// Permission restrict server-side tile modification projectiles like liquid bombs &amp; rockets, dirt bombs.
        /// </summary>
        public bool ProjectileKillMapEditRestriction = true;

        /// <summary>
        /// Restrict quick stack to have build permission.
        /// </summary>
        public bool QuickStackRestriction = true;

        /// <summary>
        /// Restrict sign edit to have build permission.
        /// </summary>
        public bool SignEditRestriction = true;
    }

    public class PermissionSettings
    {
        public PermissionLogSettings Log = new();
        public RestrictSettings Restrict = new();
        public PresetSettings Preset = new();

        public class PermissionLogSettings
        {
            public bool Enabled = true;
            public int LogCount = 50;
            public bool LogDuplicate = false;
            public double LogDistinctTime = 1;
            public bool LogStackTrace = false;
        }

        public class RestrictSettings
        {
            public bool Enabled = true;
            public bool ToggleTeam = true;
            public bool TogglePvP = true;
            public bool SyncLoadout = true;
            public bool SummonBoss = true;
        }

        public class PresetSettings
        {
            public bool Enabled = true;
            public bool AlwaysApply = false;
            public bool DebugForAdminOnly = false;
            public bool AllowRestricted = true;
        }
    }

    public class Modes
    {
        public BuildingMode Building = new();
        public PvPMode PvP = new();
        public VanillaMode Vanilla = new();

        public class BuildingMode
        {
            public bool Enabled = false;
        }

        public class PvPMode
        {
            public bool Enabled = false;
        }

        public class VanillaMode
        {
            public bool Enabled = false;
            public List<string> Permissions = new List<string> {
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
                LegacyConsts.Permissions.TogglePvP,
                LegacyConsts.Permissions.ToggleTeam,
                LegacyConsts.Permissions.SyncLoadout,
                LegacyConsts.Permissions.Ping
            };
            public bool AllowJourney = false;
            public bool IgnoreAntiCheat = false;
            public VanillaAntiCheat AntiCheat = new();

            public class VanillaAntiCheat
            {
                public bool Enabled = false;
            }
        }
    }

    public class MitigationSettings
    {
        public bool Enabled = true;

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
        /// </summary>
        public bool InventorySlotPE = true;

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
        /// </summary>
        public bool PotionSicknessPE = true;

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
        /// </summary>
        public bool SwapWhileUsePE = true;

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
        public List<LimiterConfig> ChatSpamRestrict = new List<LimiterConfig> {
            new LimiterConfig { RateLimit = 1.6, Maximum = 5 },
            new LimiterConfig { RateLimit = 4, Maximum = 20 }
        };

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
        public bool NpcUpdateBuffRateLimit = false;

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
        public bool SuppressTitle = true;

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
        public List<LimiterConfig> ConnectionLimit = new List<LimiterConfig> {
            new LimiterConfig { RateLimit = 3, Maximum = 5 },
            new LimiterConfig { RateLimit = 15, Maximum = 60 },
        };

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
        public Dictionary<int, double> ConnectionStateTimeout = new Dictionary<int, double> {
            { 0, 1 },
            { 1, 4 },
        };

        /// <summary>
        /// <para>
        /// Disabled players are restricted from most actions including being hurt.
        /// <see href="https://github.com/Pryaxis/TShock/issues/1151" />
        /// </para>
        /// <para>Cause imbalance.</para>
        /// <para>
        /// This will allow disabled players to be hurt.
        /// </para>
        /// </summary>
        public DisabledDamageAction DisabledDamageHandler = DisabledDamageAction.Preset;

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
        /// </summary>
        public ExpertCoinHandler ExpertExtraCoin = ExpertCoinHandler.Preset;

        /// <summary>
        /// <para>
        /// The legacy HttpServer.dll from .NET Framework 2.0 era is not responding correctly if
        /// the request is too long.
        /// </para>
        /// <para>Cause broken REST endpoint.</para>
        /// <para>
        /// This will try to remove the Connection header from the request.
        /// </para>
        /// </summary>
        public bool KeepRestAlive = true;

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
        /// </summary>
        public bool UseEnglishCommand = true;

        [JsonConverter(typeof(StringEnumConverter))]
        public enum DisabledDamageAction
        {
            AsIs,
            Hurt,
            Ghost,
            Preset
        }

        [JsonConverter(typeof(StringEnumConverter))]
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
            /// <summary>
            /// The suggested behavior (by the author, which is AsIs for now).
            Preset
        }
    }

    public class LimiterConfig
    {
        public double RateLimit { get; set; }
        public double Maximum { get; set; }

        public class LimiterConverter : JsonConverter<LimiterConfig>
        {
            public override void WriteJson(JsonWriter writer, LimiterConfig? value, JsonSerializer serializer)
            {
                if (value == null)
                {
                    writer.WriteValue("0/1");
                }
                else
                {
                    var str = Math.Round(value.RateLimit) == value.RateLimit ? $"{(int) value.RateLimit}" : $"{value.RateLimit}";
                    str += "/";
                    str += Math.Round(value.Maximum) == value.Maximum ? $"{(int) value.Maximum}" : $"{value.Maximum}";
                    writer.WriteValue(str);
                }
            }

            public override LimiterConfig ReadJson(JsonReader reader, Type objectType, LimiterConfig? existingValue, bool hasExistingValue, JsonSerializer serializer)
            {
                var s = reader?.Value?.ToString() ?? "0/1";
                var split = s.Split('/');
                return new LimiterConfig
                {
                    RateLimit = double.Parse(split[0]),
                    Maximum = double.Parse(split[1])
                };
            }
        }

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