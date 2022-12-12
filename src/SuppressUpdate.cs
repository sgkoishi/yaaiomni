using TerrariaApi.Server;
using TShockAPI;

namespace Chireiden.TShock.Omni;

public partial class Plugin : TerrariaPlugin
{
    public Task UpdateCheckAsync(UpdateManager um, object state)
    {
        return Task.Run(() =>
        {
            if (this.config.SuppressUpdate == UpdateOptions.Disabled)
            {
                return;
            }
            try
            {
                this.GenerateTrampoline(nameof(UpdateCheckAsync)).Invoke(um, new object[] { state });
            }
            catch when (this.config.SuppressUpdate == UpdateOptions.Silent)
            {
                // silently suppress
            }
        });
    }
}
