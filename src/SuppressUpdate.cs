using TShockAPI;

namespace Chireiden.TShock.Omni;

public partial class Plugin
{
    private async Task Detour_UpdateCheckAsync(Func<UpdateManager, object, Task> orig, UpdateManager um, object state)
    {
        var flag = this.config.Enhancements.Value.SuppressUpdate.Value;
        if (flag == Config.EnhancementsSettings.UpdateOptions.Disabled)
        {
            return;
        }
        try
        {
            await orig(um, state);
            return;
        }
        catch when (flag is Config.EnhancementsSettings.UpdateOptions.Silent)
        {
            return;
        }
    }
}