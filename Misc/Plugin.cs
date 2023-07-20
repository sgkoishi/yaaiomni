using Chireiden.TShock.Omni.Ext;
using System.Reflection;
using Terraria;
using TerrariaApi.Server;

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
        var core = ServerApi.Plugins.Get<Omni.Plugin>();
        if (core is null)
        {
            throw new Exception("Core Omni is null.");
        }
        core.OnConfigLoad += (plugin) =>
        {
            plugin.config.HideCommands.Mutate(list => list.AddRange(new List<string> {
                DefinedConsts.Commands.PvPStatus,
                DefinedConsts.Commands.TeamStatus,
                DefinedConsts.Commands.Chat,
                DefinedConsts.Commands.Echo,
            }));
            plugin.config.Mode.Value.Vanilla.Value.Permissions.Mutate(list => list.AddRange(new List<string>
            {
                DefinedConsts.Permission.TogglePvP,
                DefinedConsts.Permission.ToggleTeam,
                DefinedConsts.Permission.SyncLoadout,
            }));
        };
        core.OnPermissionSetup += (plugin) =>
        {
            var guest = TShockAPI.TShock.Groups.GetGroupByName(TShockAPI.TShock.Config.Settings.DefaultGuestGroupName);
            if (this.config.Permission.Value.Preset.Value.AllowRestricted || plugin.config.Mode.Value.Vanilla.Value.Enabled)
            {
                Omni.Utils.AddPermission(guest,
                    DefinedConsts.Permission.TogglePvP,
                    DefinedConsts.Permission.ToggleTeam,
                    DefinedConsts.Permission.SyncLoadout,
                    DefinedConsts.Permission.PvPStatus,
                    DefinedConsts.Permission.TeamStatus);

                Omni.Utils.AddPermission(guest, DefinedConsts.Permission.Echo);
                Omni.Utils.AliasPermission(TShockAPI.Permissions.canchat, DefinedConsts.Permission.Chat);
                Omni.Utils.AliasPermission(DefinedConsts.Permission.TogglePvP, $"{DefinedConsts.Permission.TogglePvP}.*");
                Omni.Utils.AliasPermission(DefinedConsts.Permission.ToggleTeam, $"{DefinedConsts.Permission.ToggleTeam}.*");

                Omni.Utils.AliasPermission(TShockAPI.Permissions.summonboss, $"{DefinedConsts.Permission.SummonBoss}.*");
                Omni.Utils.AliasPermission(TShockAPI.Permissions.startinvasion, $"{DefinedConsts.Permission.SummonBoss}.*");

                Omni.Utils.AliasPermission(TShockAPI.Permissions.kick,
                    DefinedConsts.Permission.Admin.PvPStatus,
                    DefinedConsts.Permission.Admin.TeamStatus);
            }
        };


        this.InitCommands();

        OTAPI.Hooks.MessageBuffer.GetData += this.OTHook_Permission_SyncLoadout;
        OTAPI.Hooks.MessageBuffer.GetData += this.OTHook_Permission_SummonBoss;
        TShockAPI.GetDataHandlers.TogglePvp.Register(this.GDHook_Permission_TogglePvp);
        TShockAPI.GetDataHandlers.PlayerTeam.Register(this.GDHook_Permission_PlayerTeam);
        TShockAPI.Hooks.GeneralHooks.ReloadEvent += (args) =>
        {
            this.LoadConfig(TShockAPI.TSPlayer.Server);
        };
        Utils.ConsolePlayer.Instance.SendSuccessMessage($"{this.Name} initialized.");
    }

    private void LoadConfig(TShockAPI.TSPlayer? initiator)
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
            OTAPI.Hooks.MessageBuffer.GetData -= this.OTHook_Permission_SyncLoadout;
            OTAPI.Hooks.MessageBuffer.GetData -= this.OTHook_Permission_SummonBoss;
            TShockAPI.GetDataHandlers.TogglePvp.UnRegister(this.GDHook_Permission_TogglePvp);
            TShockAPI.GetDataHandlers.PlayerTeam.UnRegister(this.GDHook_Permission_PlayerTeam);
            var asm = Assembly.GetExecutingAssembly();
            TShockAPI.Commands.ChatCommands.RemoveAll(c => c.CommandDelegate.Method?.DeclaringType?.Assembly == asm);
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