using MonoMod.RuntimeDetour;
using TerrariaApi.Server;
using TShockAPI;

public partial class Plugin : TerrariaPlugin
{
    private readonly IDetour _UpdateCheckAsyncDetour;

    public async Task UpdateCheckAsync(object state)
    {
        if (this.config.SuppressUpdate == UpdateOptions.Disabled)
        {
            return;
        }
        try
        {
            this._UpdateCheckAsyncDetour.GenerateTrampoline().Invoke(TShock.UpdateManager, new object[] { state });
        }
        catch when (this.config.SuppressUpdate == UpdateOptions.Silent)
        {
            // silently suppress
            return;
        }
    }
}
