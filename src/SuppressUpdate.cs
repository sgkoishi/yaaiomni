using MonoMod.RuntimeDetour;
using TerrariaApi.Server;
using TShockAPI;

namespace Chireiden.TShock.Omni;

public partial class Plugin : TerrariaPlugin
{
    private readonly IDetour _UpdateCheckAsyncDetour;

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
                this._UpdateCheckAsyncDetour.GenerateTrampoline().Invoke(um, new object[] { state });
            }
            catch when (this.config.SuppressUpdate == UpdateOptions.Silent)
            {
                // silently suppress
            }
        });
    }
}
