﻿using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Chireiden.TShock.Omni;

public class Config
{
    public bool SyncVersion = true;
    public bool TrimMemory = true;
    public bool ShowConfig = false;
    public string DateTimeFormat = "yyyy-MM-dd HH:mm:ss.fff";
    public UpdateOptions SuppressUpdate = UpdateOptions.Preset;
    public SocketType Socket = SocketType.Preset;
    public NameCollisionAction NameCollision = NameCollisionAction.Preset;
    public TileProviderOptions TileProvider = TileProviderOptions.Preset;
    public List<string> PlayerWildcardFormat = new List<string> {
        "*all*"
    };
    public List<string> HideCommands = new List<string> {
        Plugin.Consts.Commands.Whynot,
        Plugin.Consts.Commands.SetPvp,
        Plugin.Consts.Commands.SetTeam,
        Plugin.Consts.Commands.TriggerGarbageCollection,
        Plugin.Consts.Commands.DebugStat,
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
        /// <summary> Kick the first player </summary>
        First,
        /// <summary> Kick the second player </summary>
        Second,
        /// <summary> Kick both players </summary>
        Both,
        /// <summary> Kick neither player </summary>
        None,
        /// <summary> Kick whoever does not using a known ip and not logged in, fallback to <see cref="Second"/> </summary>
        Known,
        /// <summary> Do nothing </summary>
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
        /// <summary> Permission restrict server-side tile modification projectiles like liquid bombs & rockets, dirt bombs. </summary>
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
            public bool DebugForAdminOnly = true;
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
                Plugin.Consts.Permissions.TogglePvP,
                Plugin.Consts.Permissions.ToggleTeam,
                Plugin.Consts.Permissions.SyncLoadout,
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
        public bool InventorySlotPE = true;
    }
}
