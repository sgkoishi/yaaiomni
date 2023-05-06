using System.Runtime.CompilerServices;
using Terraria.Localization;
using TerrariaApi.Server;
using static TShockAPI.GetDataHandlers;

namespace Chireiden.TShock.Omni;

partial class Plugin : TerrariaPlugin
{
    private void GDHook_Permission_TogglePvp(object? sender, TogglePvpEventArgs args)
    {
        if (args.Handled)
        {
            return;
        }

        var restrict = this.config.Permission.Restrict;
        if (!restrict.Enabled || !restrict.TogglePvP)
        {
            return;
        }

        if (!args.Player.HasPermission(LegacyConsts.Permissions.TogglePvP))
        {
            TShockAPI.TShock.Log.ConsoleDebug($"Player {args.Player.Name} tried to toggle PvP to {args.Pvp} without permission {LegacyConsts.Permissions.TogglePvP}.");
            args.Handled = true;
            Terraria.NetMessage.TrySendData((int) PacketTypes.TogglePvp, -1, -1, NetworkText.Empty, args.PlayerId);
            return;
        }

        if (!args.Player.HasPermission($"{LegacyConsts.Permissions.TogglePvP}.{args.Pvp}"))
        {
            TShockAPI.TShock.Log.ConsoleDebug($"Player {args.Player.Name} tried to toggle PvP to {args.Pvp} without permission {LegacyConsts.Permissions.TogglePvP}.{args.Pvp}.");
            args.Handled = true;
            Terraria.NetMessage.TrySendData((int) PacketTypes.TogglePvp, -1, -1, NetworkText.Empty, args.PlayerId);
            return;
        }
    }

    private void GDHook_Permission_PlayerTeam(object? sender, PlayerTeamEventArgs args)
    {
        if (args.Handled)
        {
            return;
        }

        var restrict = this.config.Permission.Restrict;
        if (!restrict.Enabled || !restrict.ToggleTeam)
        {
            return;
        }

        if (!args.Player.HasPermission(LegacyConsts.Permissions.ToggleTeam))
        {
            TShockAPI.TShock.Log.ConsoleDebug($"Player {args.Player.Name} tried to toggle team to {args.Team} without permission {LegacyConsts.Permissions.ToggleTeam}.");
            args.Handled = true;
            Terraria.NetMessage.TrySendData((int) PacketTypes.PlayerTeam, -1, -1, NetworkText.Empty, args.PlayerId);
            return;
        }

        if (!args.Player.HasPermission($"{LegacyConsts.Permissions.ToggleTeam}.{args.Team}"))
        {
            TShockAPI.TShock.Log.ConsoleDebug($"Player {args.Player.Name} tried to toggle team to {args.Team} without permission {LegacyConsts.Permissions.ToggleTeam}.{args.Team}.");
            args.Handled = true;
            Terraria.NetMessage.TrySendData((int) PacketTypes.PlayerTeam, -1, -1, NetworkText.Empty, args.PlayerId);
            return;
        }
    }

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

        var restrict = this.config.Permission.Restrict;
        if (!restrict.Enabled || !restrict.SyncLoadout)
        {
            return;
        }

        var player = TShockAPI.TShock.Players[args.Instance.whoAmI];
        if (player == null || player.HasPermission(LegacyConsts.Permissions.SyncLoadout))
        {
            return;
        }

        TShockAPI.TShock.Log.ConsoleDebug($"Player {player.Name} tried to sync loadout without permission {LegacyConsts.Permissions.SyncLoadout}.");
        args.Result = OTAPI.HookResult.Cancel;
        // FIXME: TSAPI is not respecting args.Result, so we have to craft invalid packet. Switch to Loadout 255 when only 3.
        args.PacketId = byte.MaxValue;
        Terraria.NetMessage.TrySendData((int) PacketTypes.SyncLoadout, -1, -1, null, args.Instance.whoAmI, player.TPlayer.CurrentLoadoutIndex);
    }

    private void OTHook_Permission_SummonBoss(object? sender, OTAPI.Hooks.MessageBuffer.GetDataEventArgs args)
    {
        if (args.Result == OTAPI.HookResult.Cancel)
        {
            return;
        }

        var restrict = this.config.Permission.Restrict;
        if (!restrict.Enabled || !restrict.SummonBoss)
        {
            return;
        }

        if (args.PacketId == (int) PacketTypes.NpcStrike)
        {
            var index = BitConverter.ToInt16(args.Instance.readBuffer.AsSpan(args.ReadOffset, 2));
            if (index == Terraria.ID.NPCID.EmpressButterfly || index == Terraria.ID.NPCID.CultistDevote || index == Terraria.ID.NPCID.CultistArcherBlue)
            {
                if (!TShockAPI.TShock.Players[args.Instance.whoAmI].HasPermission($"{LegacyConsts.Permissions.SummonBoss}.{index}"))
                {
                    TShockAPI.TShock.Log.ConsoleDebug($"Player {TShockAPI.TShock.Players[args.Instance.whoAmI].Name} tried to summon boss {index} without permission {LegacyConsts.Permissions.SummonBoss}.{index}.");
                    Terraria.NetMessage.TrySendData((int) PacketTypes.NpcUpdate, args.Instance.whoAmI, -1, null, index);
                    args.Result = OTAPI.HookResult.Cancel;
                }
            }
        }
        else if (args.PacketId == (int) PacketTypes.SpawnBossorInvasion)
        {
            var id = BitConverter.ToInt16(args.Instance.readBuffer.AsSpan(args.ReadOffset + 2, 2));
            if (!TShockAPI.TShock.Players[args.Instance.whoAmI].HasPermission($"{LegacyConsts.Permissions.SummonBoss}.{id}"))
            {
                TShockAPI.TShock.Log.ConsoleDebug($"Player {TShockAPI.TShock.Players[args.Instance.whoAmI].Name} tried to summon boss {id} without permission {LegacyConsts.Permissions.SummonBoss}.{id}.");
                args.Result = OTAPI.HookResult.Cancel;
            }
        }
        else if (args.PacketId == (int) PacketTypes.FishOutNPC)
        {
            var id = BitConverter.ToInt16(args.Instance.readBuffer.AsSpan(args.ReadOffset + 4, 2));
            if (id == Terraria.ID.NPCID.DukeFishron && !TShockAPI.TShock.Players[args.Instance.whoAmI].HasPermission($"{LegacyConsts.Permissions.SummonBoss}.{id}"))
            {
                TShockAPI.TShock.Log.ConsoleDebug($"Player {TShockAPI.TShock.Players[args.Instance.whoAmI].Name} tried to summon boss {id} without permission {LegacyConsts.Permissions.SummonBoss}.{id}.");
                args.Result = OTAPI.HookResult.Cancel;
            }
        }

        if (args.Result == OTAPI.HookResult.Cancel)
        {
            // FIXME: TSAPI is not respecting args.Result, so we have to craft invalid packet.
            args.PacketId = byte.MaxValue;
        }
    }
}