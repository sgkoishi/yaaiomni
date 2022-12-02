using MonoMod.RuntimeDetour;
using TerrariaApi.Server;
using TShockAPI;

public partial class Plugin : TerrariaPlugin
{
    private readonly IDetour _checkForUpdateDetour;

    internal async Task CheckForUpdatesAsync(object state)
    {
        if (!this.config.SuppressUpdate)
        {
            this._checkForUpdateDetour.GenerateTrampoline().Invoke(TShock.UpdateManager, new object[] { state });
        }
    }
}
