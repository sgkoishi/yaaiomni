using Chireiden.TShock.Omni.DefinedConsts;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Chireiden.TShock.Omni;

public partial class Plugin
{
    private readonly ConditionalWeakTable<TShockAPI.TSPlayer, AttachedData> _playerData = new ConditionalWeakTable<TShockAPI.TSPlayer, AttachedData>();

    public AttachedData? this[TShockAPI.TSPlayer player]
    {
        get
        {
            if (player == null)
            {
                return null;
            }
            if (this._playerData.TryGetValue(player, out var data))
            {
                return data;
            }
            data = new AttachedData(player, this.config.Permission.Value.Log.Value.LogCount, this.config.Mitigation.Value.ChatSpamRestrict);
            this._playerData.Add(player, data);
            return data;
        }
    }
    public AttachedData? this[int player] => this[TShockAPI.TShock.Players[player]];
    public AttachedData? this[Terraria.Player player] => this[player.whoAmI];
}

public class AttachedData
{
    internal TShockAPI.TSPlayer Player;
    public bool? Ghost;
    internal int DetectPE = 1;
    internal int PendingRevertHeal;
    public Limiter[] ChatSpamRestrict;
    public Ring<PermissionCheckHistory> PermissionHistory;
    internal PendingAck[] RecentPings;
    public Action<TimeSpan>? OnPingUpdated;
    public List<DelayCommand> DelayCommands;
    public int PermissionBypass;

    public bool IsPE
    {
        get => this.DetectPE >= 500;
        internal set => this.Player.SetData(DataKey.IsPE, true);
    }

    public TimeSpan LastPing
    {
        get
        {
            var last = this.RecentPings.Where(p => p.Start != null && p.End != null).OrderBy(p => p.Start).Last();
            return last.End!.Value - last.Start!.Value;
        }
    }

    public AttachedData(TShockAPI.TSPlayer player, int logCount, List<Config.LimiterConfig> chatLimiter)
    {
        this.Player = player;
        this.PermissionHistory = new Ring<PermissionCheckHistory>(logCount);
        this.RecentPings = Terraria.Main.item.Select(_ => new PendingAck()).ToArray();
        this.DelayCommands = new List<DelayCommand>();
        this.ChatSpamRestrict = chatLimiter.Select(lc => (Limiter) lc).ToArray();
    }

    public record PermissionCheckHistory(string Permission, DateTime Time, bool Result, StackTrace? Trace);

    internal class PendingAck
    {
        internal DateTime? Start;
        internal DateTime? End;
        internal Action<TShockAPI.TSPlayer, TimeSpan>? Callback;
    }

    public class DelayCommand
    {
        public string Command { get; set; }
        public int Timeout { get; set; }
        public int Start { get; set; }
        public int Repeat { get; set; }
        public DelayCommand(string command, int start = 0, int timeout = 60, int repeat = 1)
        {
            this.Command = command;
            this.Timeout = timeout;
            this.Start = start;
            this.Repeat = repeat;
        }
    }
}