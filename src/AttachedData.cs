using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using TerrariaApi.Server;

namespace Chireiden.TShock.Omni;

public partial class Plugin : TerrariaPlugin
{
    private readonly ConditionalWeakTable<TShockAPI.TSPlayer, AttachedData> _playerData = new ConditionalWeakTable<TShockAPI.TSPlayer, AttachedData>();
    public AttachedData this[TShockAPI.TSPlayer player]
    {
        get
        {
            if (this._playerData.TryGetValue(player, out var data))
            {
                return data;
            }
            data = new AttachedData(player, this.config.Mitigation.ChatSpamRestrict.Count);
            this._playerData.Add(player, data);
            return data;
        }
    }
    public AttachedData this[int player] => this[TShockAPI.TShock.Players[player]];
    public AttachedData this[Terraria.Player player] => this[player.whoAmI];
}

public class AttachedData
{
    internal TShockAPI.TSPlayer Player;
    public bool? Ghost;
    public bool IsPE
    {
        get => this.DetectPE >= 500;
        internal set => this.Player.SetData(Consts.DataKey.IsPE, true);
    }
    internal int DetectPE = 1;
    internal int PendingRevertHeal;
    internal double[] ChatSpamRestrict;
    public Queue<PermissionCheckHistory> PermissionHistory;
    public PingData PingChannel;
    public List<DelayCommand> DelayCommands;
    public int PermissionBypass;

    public AttachedData(TShockAPI.TSPlayer player, int chatLimiter)
    {
        this.Player = player;
        this.ChatSpamRestrict = new double[chatLimiter];
        this.PermissionHistory = new Queue<PermissionCheckHistory>();
        this.PingChannel = new PingData();
        this.DelayCommands = new List<DelayCommand>();
    }

    public record PermissionCheckHistory(string Permission, DateTime Time, bool Result, StackTrace? Trace);

    public class PingData
    {
        public TimeSpan? LastPing;
        internal PingDetails?[] RecentPings = new PingDetails?[Terraria.Main.item.Length];
    }

    internal class PingDetails
    {
        internal Channel<int>? Channel;
        internal DateTime Start = DateTime.Now;
        internal DateTime? End = null;
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
            this.Start = this.Timeout;
            this.Repeat = repeat;
        }
    }
}