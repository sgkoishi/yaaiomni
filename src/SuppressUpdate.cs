using TShockAPI;

namespace Chireiden.TShock.Omni;

public partial class Plugin
{
    private async Task Detour_UpdateCheckAsync(Func<UpdateManager, object, Task> orig, UpdateManager um, object state)
    {
        if (this.config.Enhancements.SuppressUpdate == Config.EnhancementsSettings.UpdateOptions.Disabled)
        {
            return;
        }
        try
        {
            await orig(um, state);
            return;
        }
        catch when (this.config.Enhancements.SuppressUpdate
            is Config.EnhancementsSettings.UpdateOptions.Silent
                or Config.EnhancementsSettings.UpdateOptions.Preset)
        {
            return;
        }
    }
}