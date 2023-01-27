using TerrariaApi.Server;
using TShockAPI;

namespace Chireiden.TShock.Omni;

public partial class Plugin : TerrariaPlugin
{
    private async Task Detour_UpdateCheckAsync(Func<UpdateManager, object, Task> orig, UpdateManager um, object state)
    {
        if (this.config.SuppressUpdate == Config.UpdateOptions.Disabled)
        {
            return;
        }
        try
        {
            await orig(um, state);
            return;
        }
        catch when (this.config.SuppressUpdate is Config.UpdateOptions.Silent or Config.UpdateOptions.Preset)
        {
            return;
        }
    }
}
