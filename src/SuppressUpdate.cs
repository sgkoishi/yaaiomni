using MonoMod.RuntimeDetour;
using TerrariaApi.Server;
using TShockAPI;

public partial class Plugin : TerrariaPlugin
{
    private readonly IDetour _UpdateCheckAsyncDetour;

    public async Task UpdateCheckAsync(UpdateManager um, object state)
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
            return;
        }
    }
}
