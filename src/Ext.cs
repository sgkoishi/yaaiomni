using System.Collections.ObjectModel;

namespace TerrariaApi.Server;

public static class OmniExtensions
{
    public static T? Get<T>(this ReadOnlyCollection<PluginContainer> plugins) where T : TerrariaPlugin
    {
        foreach (var plugin in plugins)
        {
            if (plugin.Plugin is T t)
            {
                return t;
            }
        }

        return default;
    }
}