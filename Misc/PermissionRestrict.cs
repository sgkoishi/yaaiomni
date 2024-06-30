using Terraria.Localization;
using TerrariaApi.Server;

namespace Chireiden.TShock.Omni.Misc;

public partial class Plugin : TerrariaPlugin
{
    [RelatedPermission("TogglePvP", "chireiden.omni.togglepvp")]
    private void GDHook_Permission_TogglePvp(object? sender, TShockAPI.GetDataHandlers.TogglePvpEventArgs args)
    {
        if (args.Handled)
        {
            return;
        }

        var restrict = this.config.Permission.Value.Restrict.Value;
        if (!restrict.Enabled || !restrict.TogglePvP)
        {
            return;
        }

        if (!args.Player.HasPermission(DefinedConsts.Permission.TogglePvP))
        {
            TShockAPI.TShock.Log.ConsoleDebug($"Player {args.Player.Name} tried to toggle PvP to {args.Pvp} without permission {DefinedConsts.Permission.TogglePvP}.");
            args.Handled = true;
            Terraria.NetMessage.TrySendData((int) PacketTypes.TogglePvp, -1, -1, NetworkText.Empty, args.PlayerId);
            return;
        }

        if (!args.Player.HasPermission($"{DefinedConsts.Permission.TogglePvP}.{args.Pvp}"))
        {
            TShockAPI.TShock.Log.ConsoleDebug($"Player {args.Player.Name} tried to toggle PvP to {args.Pvp} without permission {DefinedConsts.Permission.TogglePvP}.{args.Pvp}.");
            args.Handled = true;
            Terraria.NetMessage.TrySendData((int) PacketTypes.TogglePvp, -1, -1, NetworkText.Empty, args.PlayerId);
            return;
        }
    }

    [RelatedPermission("ToggleTeam", "chireiden.omni.toggleteam")]
    private void GDHook_Permission_PlayerTeam(object? sender, TShockAPI.GetDataHandlers.PlayerTeamEventArgs args)
    {
        if (args.Handled)
        {
            return;
        }

        var restrict = this.config.Permission.Value.Restrict.Value;
        if (!restrict.Enabled || !restrict.ToggleTeam)
        {
            return;
        }

        if (!args.Player.HasPermission(DefinedConsts.Permission.ToggleTeam))
        {
            TShockAPI.TShock.Log.ConsoleDebug($"Player {args.Player.Name} tried to toggle team to {args.Team} without permission {DefinedConsts.Permission.ToggleTeam}.");
            args.Handled = true;
            Terraria.NetMessage.TrySendData((int) PacketTypes.PlayerTeam, -1, -1, NetworkText.Empty, args.PlayerId);
            return;
        }

        if (!args.Player.HasPermission($"{DefinedConsts.Permission.ToggleTeam}.{args.Team}"))
        {
            TShockAPI.TShock.Log.ConsoleDebug($"Player {args.Player.Name} tried to toggle team to {args.Team} without permission {DefinedConsts.Permission.ToggleTeam}.{args.Team}.");
            args.Handled = true;
            Terraria.NetMessage.TrySendData((int) PacketTypes.PlayerTeam, -1, -1, NetworkText.Empty, args.PlayerId);
            return;
        }
    }

    [RelatedPermission("SyncLoadout", "chireiden.omni.syncloadout")]
    private void OTHook_Permission_SyncLoadout(object? sender, OTAPI.Hooks.MessageBuffer.GetDataEventArgs args)
    {
        if (args.Result == OTAPI.HookResult.Cancel)
        {
            return;
        }

        if (args.PacketId != (int) PacketTypes.SyncLoadout)
        {
            return;
        }

        var restrict = this.config.Permission.Value.Restrict.Value;
        if (!restrict.Enabled || !restrict.SyncLoadout)
        {
            return;
        }

        var player = TShockAPI.TShock.Players[args.Instance.whoAmI];
        if (player?.HasPermission(DefinedConsts.Permission.SyncLoadout) != false)
        {
            return;
        }

        TShockAPI.TShock.Log.ConsoleDebug($"Player {player.Name} tried to sync loadout without permission {DefinedConsts.Permission.SyncLoadout}.");
        args.CancelPacket();
        Terraria.NetMessage.TrySendData((int) PacketTypes.SyncLoadout, -1, -1, null, args.Instance.whoAmI, player.TPlayer.CurrentLoadoutIndex);
    }

    [RelatedPermission("SummonBoss", "chireiden.omni.summonboss")]
    private void OTHook_Permission_SummonBoss(object? sender, OTAPI.Hooks.MessageBuffer.GetDataEventArgs args)
    {
        if (args.Result == OTAPI.HookResult.Cancel)
        {
            return;
        }

        var restrict = this.config.Permission.Value.Restrict.Value;
        if (!restrict.Enabled || !restrict.SummonBoss)
        {
            return;
        }

        if (args.PacketId == (int) PacketTypes.NpcStrike)
        {
            var index = args.Read<short>(0);
            if (index == Terraria.ID.NPCID.EmpressButterfly || index == Terraria.ID.NPCID.CultistDevote || index == Terraria.ID.NPCID.CultistArcherBlue)
            {
                if (!TShockAPI.TShock.Players[args.Instance.whoAmI].HasPermission($"{DefinedConsts.Permission.SummonBoss}.{index}"))
                {
                    TShockAPI.TShock.Log.ConsoleInfo($"Player {TShockAPI.TShock.Players[args.Instance.whoAmI].Name} tried to summon boss {index} without permission {DefinedConsts.Permission.SummonBoss}.{index}.");
                    Terraria.NetMessage.TrySendData((int) PacketTypes.NpcUpdate, args.Instance.whoAmI, -1, null, index);
                    args.Result = OTAPI.HookResult.Cancel;
                }
            }
        }
        else if (args.PacketId == (int) PacketTypes.SpawnBossorInvasion)
        {
            var id = args.Read<short>(2);
            if (!TShockAPI.TShock.Players[args.Instance.whoAmI].HasPermission($"{DefinedConsts.Permission.SummonBoss}.{id}"))
            {
                TShockAPI.TShock.Log.ConsoleInfo($"Player {TShockAPI.TShock.Players[args.Instance.whoAmI].Name} tried to summon boss {id} without permission {DefinedConsts.Permission.SummonBoss}.{id}.");
                args.Result = OTAPI.HookResult.Cancel;
            }
        }
        else if (args.PacketId == (int) PacketTypes.FishOutNPC)
        {
            var id = args.Read<short>(4);
            if (id == Terraria.ID.NPCID.DukeFishron && !TShockAPI.TShock.Players[args.Instance.whoAmI].HasPermission($"{DefinedConsts.Permission.SummonBoss}.{id}"))
            {
                TShockAPI.TShock.Log.ConsoleInfo($"Player {TShockAPI.TShock.Players[args.Instance.whoAmI].Name} tried to summon boss {id} without permission {DefinedConsts.Permission.SummonBoss}.{id}.");
                args.Result = OTAPI.HookResult.Cancel;
            }
        }

        if (args.Result == OTAPI.HookResult.Cancel)
        {
            args.CancelPacket();
        }
    }
}