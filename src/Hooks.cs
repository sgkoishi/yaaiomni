using MonoMod.RuntimeDetour;
using System.Reflection;
using TerrariaApi.Server;

namespace Chireiden.TShock.Omni;

public partial class Plugin : TerrariaPlugin
{
    private readonly Dictionary<string, IDetour> _detours = new();
    internal void Detour(string name, MethodBase from, Delegate to)
    {
        this._detours.Add(name, new Hook(from, to));
    }
}
