﻿using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Chireiden.TShock.Omni;

/// <summary>
/// This is the config file for Omni.
/// </summary>
public class Config
{
    /// <summary>
    /// Disable vanilla version check.
    /// </summary>
    public bool SyncVersion = true;
    /// <summary> Trim memory depends on the world. No side effect. </summary>
    public bool TrimMemory = true;
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
        Consts.Commands.Whynot,
        Consts.Commands.SetPvp,
        Consts.Commands.SetTeam,
        Consts.Commands.TriggerGarbageCollection,
        Consts.Commands.DebugStat,
        Consts.Commands.ResetCharacter,
        Consts.Commands.Ping,
    };
    public Dictionary<string, List<string>> CommandRenames = new();
    public LavaSettings LavaHandler = new();
    public DebugPacketSettings DebugPacket = new();
    public SoundnessSettings Soundness = new();
    public PermissionSettings Permission = new();
    public Modes Mode = new();
    public MitigationSettings Mitigation = new();

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
        /// <summary> Permission restrict server-side tile modification projectiles like liquid bombs &amp; rockets, dirt bombs. </summary>
        public bool ProjectileKillMapEditRestriction = true;
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
        }

        public class PresetSettings
        {
            public bool Enabled = true;
            public bool DebugForAdminOnly = false;
            public bool Restrict = true;
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
                Consts.Permissions.TogglePvP,
                Consts.Permissions.ToggleTeam,
                Consts.Permissions.SyncLoadout,
                Consts.Permissions.Ping
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
        ///   3 messages per 5 seconds
        ///   5 messages per 20 seconds
        /// </para>
        /// </summary>
        public List<(int RateLimit, int Maximum)> ChatSpamRestrict = new List<(int RateLimit, int Maximum)> {
            (100, 300),
            (240, 1200)
        };

        /// <summary>
        /// <para>
        /// Restrict the rate of sending <see cref="PacketTypes.NpcUpdateBuff"/>.
        /// In some cases, the client will send <see cref="PacketTypes.NpcAddBuff"/> frequently,
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
        /// linux should support OSC commands according to the implementation of <seealso cref="Console.Title"/>, but some doesn't.
        /// https://source.dot.net/#System.Console/System/TerminalFormatStrings.cs,e0a3bdd93a9caf05,references
        /// </para>
        /// <para>Cause spam in console.</para>
        /// <para>
        /// This will prevent the title from being set if TERM has no xterm.
        /// </para>
        /// </summary>
        public bool SuppressTitle = true;
    }
}
