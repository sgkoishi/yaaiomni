using Chireiden.TShock.Omni.Ext;
using System.Reflection;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace Chireiden.TShock.Omni.Misc;

[ApiVersion(2, 1)]
public partial class Plugin : TerrariaPlugin
{
    public override string Name => $"{Assembly.GetExecutingAssembly().GetName().Name} {CommitHashAttribute.GetCommitHash()[0..10]}";
    public override string Author => "SGKoishi";
    public override Version Version => Assembly.GetExecutingAssembly().GetName().Version!;
    public override string Description => Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description!;
    public override string UpdateURL => Assembly.GetExecutingAssembly()
        .GetCustomAttributes<AssemblyMetadataAttribute>()
        .FirstOrDefault(x => x.Key == "RepositoryUrl")?.Value!;

    public string ConfigPath = Path.Combine(TShockAPI.TShock.SavePath, "chireiden.omni.misc.json");
    private const BindingFlags _bfany = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    public Config config;

    public Plugin(Main game) : base(game)
    {
        this.config = new Config();
        this.LoadConfig(Utils.ConsolePlayer.Instance);
        this.Detour(
            nameof(this.Detour_Lava_HitEffect),
            typeof(NPC)
                .GetMethod(nameof(NPC.HitEffect), _bfany),
            this.Detour_Lava_HitEffect);
        this.Detour(
            nameof(this.Detour_Lava_KillTile),
            typeof(WorldGen)
                .GetMethod(nameof(WorldGen.KillTile), _bfany),
            this.Detour_Lava_KillTile);
    }

    public override void Initialize()
    {
        ServerApi.Plugins.Get<Omni.Plugin>()!.OnConfigLoad += (plugin) =>
        {
            plugin.config.HideCommands.Mutate(list => list.AddRange(new List<string> {
                DefinedConsts.Commands.PvPStatus,
                DefinedConsts.Commands.TeamStatus,
            }));
        };

        this.InitCommands();

        TShockAPI.Hooks.GeneralHooks.ReloadEvent += (args) => { };
    }

    private void LoadConfig(TSPlayer? initiator)
    {
        try
        {
            if (File.Exists(this.ConfigPath))
            {
                this.config = Json.JsonUtils.DeserializeConfig<Config>(File.ReadAllText(this.ConfigPath));
            }
        }
        catch (Exception ex)
        {
            initiator?.SendErrorMessage($"Failed to load config: {ex.Message}");
            return;
        }

        try
        {
            if (!Directory.Exists(TShockAPI.TShock.SavePath))
            {
                Directory.CreateDirectory(TShockAPI.TShock.SavePath);
            }
            File.WriteAllText(this.ConfigPath, Json.JsonUtils.SerializeConfig(this.config));
        }
        catch (Exception ex)
        {
            initiator?.SendErrorMessage($"Failed to save config: {ex.Message}");
            return;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            var asm = Assembly.GetExecutingAssembly();
            Commands.ChatCommands.RemoveAll(c => c.CommandDelegate.Method?.DeclaringType?.Assembly == asm);
            foreach (var detour in this._detours.Values)
            {
                detour.Undo();
                detour.Dispose();
            }
            this._detours.Clear();
            foreach (var hook in this._manipulators.Values)
            {
                hook.Undo();
                hook.Dispose();
            }
            this._manipulators.Clear();
        }
        base.Dispose(disposing);
    }
}