namespace Chireiden.TShock.Omni.Misc;

/// <summary>
/// This is the config file for Omni.
/// </summary>
public class Config
{
    public Optional<EnhancementsSettings> Enhancements = Optional.Default(new EnhancementsSettings());

    public Optional<LavaSettings> LavaHandler = Optional.Default(new LavaSettings(), true);

    public Optional<PermissionSettings> Permission = Optional.Default(new PermissionSettings());

    public record class EnhancementsSettings
    {
        /// <summary>
        /// Disable vanilla version check.
        /// </summary>
        public Optional<bool> SyncVersion = Optional.Default(false);
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

    public record class PermissionSettings
    {
        public Optional<RestrictSettings> Restrict = Optional.Default(new RestrictSettings(), true);
        public Optional<PresetSettings> Preset = Optional.Default(new PresetSettings());
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
            public Optional<bool> AllowRestricted = Optional.Default(true, true);
        }
    }
}