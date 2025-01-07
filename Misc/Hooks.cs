using MonoMod.RuntimeDetour;
using System.Reflection;

namespace Chireiden.TShock.Omni.Misc;

partial class Plugin
{
    private readonly Dictionary<string, Hook> _detours = new();
    internal void Detour(string name, MethodBase? from, Delegate to)
    {
        if (from is null)
        {
            Utils.ShowError($"Hook (Detour) failed for \"{name}\": source is null");
        }
        else
        {
            this._detours.Add(name, new Hook(from, to));
        }
    }

    private readonly Dictionary<string, ILHook> _manipulators = new();
    internal void ILHook(string name, MethodBase? from, MonoMod.Cil.ILContext.Manipulator to)
    {
        if (from is null)
        {
            Utils.ShowError($"Hook (Manipulate) failed for \"{name}\": source is null");
        }
        else
        {
            this._manipulators.Add(name, new ILHook(from, to));
        }
    }
}