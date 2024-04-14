using Chireiden.TShock.Omni.Ext;
using System.Reflection;
using Terraria;
using TerrariaApi.Server;

namespace Chireiden.TShock.Omni.Misc;

[ApiVersion(2, 1)]
public partial class Plugin : TerrariaPlugin
{
    public override string Name => $"{Assembly.GetExecutingAssembly().GetName().Name} {new string(CommitHashAttribute.GetCommitHash().Take(10).ToArray())}";
    public override string Author => "SGKoishi";
    public override Version Version => Assembly.GetExecutingAssembly().GetName().Version!;
    public override string Description => Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description!;
    public override string UpdateURL => Assembly.GetExecutingAssembly()
        .GetCustomAttributes<AssemblyMetadataAttribute>()
        .FirstOrDefault(x => x.Key == "RepositoryUrl")?.Value!;

    public string ConfigPath = Path.Combine(TShockAPI.TShock.SavePath, "chireiden.omni.misc.json");
    private const BindingFlags _bfany = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    public Config config = null!;

    public Plugin(Main game) : base(game)
    {
    }

    public override void Initialize()
    {
        Utils.AssemblyMutex(this);
        this.config = new Config();
        this.LoadConfig(Utils.ConsolePlayer.Instance);
        var core = ServerApi.Plugins.Get<Omni.Plugin>() ?? throw new Exception("Core Omni is null.");
        Utils.OnceFlag("chireiden.omni.misc.preset.lock", () =>
        {
            core.config.HideCommands.Mutate(list => list.AddRange(new List<string> {
                DefinedConsts.Commands.PvPStatus,
                DefinedConsts.Commands.TeamStatus,
                DefinedConsts.Commands.Chat,
                DefinedConsts.Commands.Admin.GarbageCollect,
                DefinedConsts.Commands.Admin.UpsCheck,
                DefinedConsts.Commands.Admin.SqliteVacuum,
            }));
            core.config.Mode.Value.Vanilla.Value.Permissions.Mutate(list => list.AddRange(new List<string>
            {
                DefinedConsts.Permission.TogglePvP,
                DefinedConsts.Permission.ToggleTeam,
                DefinedConsts.Permission.SyncLoadout,
            }));
        });
        core.OnPermissionSetup += (plugin) =>
        {
            var guest = TShockAPI.TShock.Groups.GetGroupByName(TShockAPI.TShock.Config.Settings.DefaultGuestGroupName);
            if (this.config.Permission.Value.Preset.Value.AllowRestricted || plugin.config.Mode.Value.Vanilla.Value.Enabled)
            {
                Utils.AddPermission(guest,
                    DefinedConsts.Permission.TogglePvP,
                    DefinedConsts.Permission.ToggleTeam,
                    DefinedConsts.Permission.SyncLoadout,
                    DefinedConsts.Permission.PvPStatus,
                    DefinedConsts.Permission.TeamStatus);

                Utils.AliasPermission(TShockAPI.Permissions.canchat, DefinedConsts.Permission.Chat);
                Utils.AliasPermission(DefinedConsts.Permission.TogglePvP, $"{DefinedConsts.Permission.TogglePvP}.*");
                Utils.AliasPermission(DefinedConsts.Permission.ToggleTeam, $"{DefinedConsts.Permission.ToggleTeam}.*");

                Utils.AliasPermission(TShockAPI.Permissions.summonboss, $"{DefinedConsts.Permission.SummonBoss}.*");
                Utils.AliasPermission(TShockAPI.Permissions.startinvasion, $"{DefinedConsts.Permission.SummonBoss}.*");

                Utils.AliasPermission(TShockAPI.Permissions.kick,
                    DefinedConsts.Permission.Admin.PvPStatus,
                    DefinedConsts.Permission.Admin.TeamStatus,
                    DefinedConsts.Permission.Admin.UpsCheck);

                Utils.AliasPermission(TShockAPI.Permissions.maintenance,
                    DefinedConsts.Permission.Admin.GarbageCollect,
                    DefinedConsts.Permission.Admin.RawBroadcast,
                    DefinedConsts.Permission.Admin.TerminateSocket,
                    DefinedConsts.Permission.Admin.GenerateFullConfig,
                    DefinedConsts.Permission.Admin.SqliteVacuum,
                    DefinedConsts.Permission.Admin.FindCommand);

                Utils.AliasPermission(TShockAPI.Permissions.su,
                    DefinedConsts.Permission.Admin.ListClients,
                    DefinedConsts.Permission.Admin.DumpBuffer);
            }
        };

        this.InitCommands();

        On.Terraria.NPC.HitEffect += this.Detour_Lava_HitEffect;
        On.Terraria.MessageBuffer.GetData += this.MMHook_PatchVersion_GetData;
        On.Terraria.WorldGen.KillTile += this.Detour_Lava_KillTile;
        On.Terraria.Netplay.UpdateConnectedClients += this.Detour_UpdateConnectedClients;
        OTAPI.Hooks.MessageBuffer.GetData += this.OTHook_Permission_SyncLoadout;
        OTAPI.Hooks.MessageBuffer.GetData += this.OTHook_Permission_SummonBoss;
        TShockAPI.GetDataHandlers.TogglePvp.Register(this.GDHook_Permission_TogglePvp);
        TShockAPI.GetDataHandlers.PlayerTeam.Register(this.GDHook_Permission_PlayerTeam);
        TShockAPI.Hooks.GeneralHooks.ReloadEvent += (args) => this.LoadConfig(TShockAPI.TSPlayer.Server);
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
        }

        this.config ??= new Config();

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
            On.Terraria.NPC.HitEffect -= this.Detour_Lava_HitEffect;
            On.Terraria.MessageBuffer.GetData -= this.MMHook_PatchVersion_GetData;
            On.Terraria.WorldGen.KillTile -= this.Detour_Lava_KillTile;
            On.Terraria.Netplay.UpdateConnectedClients -= this.Detour_UpdateConnectedClients;
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