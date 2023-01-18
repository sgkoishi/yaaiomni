using TerrariaApi.Server;
using TShockAPI;

namespace Chireiden.TShock.Omni;

public partial class Plugin : TerrariaPlugin
{
    private Task Hook_UpdateCheckAsync(Func<UpdateManager, object, Task> orig, UpdateManager um, object state)
    {
        return Task.Run(() =>
        {
            if (this.config.SuppressUpdate == Config.UpdateOptions.Disabled)
            {
                return;
            }
            try
            {
                orig(um, state);
            }
            catch when (this.config.SuppressUpdate is Config.UpdateOptions.Silent or Config.UpdateOptions.Preset)
            {
                // silently suppress
            }
        });
    }
}
