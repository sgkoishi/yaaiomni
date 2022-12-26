using TerrariaApi.Server;
using TShockAPI;

namespace Chireiden.TShock.Omni;

public partial class Plugin : TerrariaPlugin
{
    public Task UpdateCheckAsync(Func<UpdateManager, object, Task> orig, UpdateManager um, object state)
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
            catch when (this.config.SuppressUpdate == Config.UpdateOptions.Silent)
            {
                // silently suppress
            }
        });
    }
}
