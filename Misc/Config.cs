using Chireiden.TShock.Omni;

namespace Chireiden.TShock.Omni.Misc;

/// <summary>
/// This is the config file for Omni.
/// </summary>
public class Config
{
    public Optional<LavaSettings> LavaHandler = Optional.Default(new LavaSettings(), true);

    public record class LavaSettings
    {
        public Optional<bool> Enabled = Optional.Default(false);
        public Optional<bool> AllowHellstone = Optional.Default(false);
        public Optional<bool> AllowCrispyHoneyBlock = Optional.Default(false);
        public Optional<bool> AllowHellbat = Optional.Default(false);
        public Optional<bool> AllowLavaSlime = Optional.Default(false);
        public Optional<bool> AllowLavabat = Optional.Default(false);
    }
}