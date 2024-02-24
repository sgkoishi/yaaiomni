using System.Collections.Concurrent;
using System.Net;
using System.Runtime.CompilerServices;

namespace Chireiden.TShock.Omni;

public partial class Plugin
{
    internal class ConnectionStore
    {
        public ConcurrentDictionary<string, Connection> Connections { get; } = new ConcurrentDictionary<string, Connection>();
        public ConditionalWeakTable<Terraria.Net.Sockets.ISocket, AttachedRawData> AttachedData { get; } = new ConditionalWeakTable<Terraria.Net.Sockets.ISocket, AttachedRawData>();

        internal class Connection
        {
            public required IPAddress Address { get; set; }
            public required ConcurrentBag<Limiter> Limit { init; get; }
        }

        public void PurgeCache()
        {
            var alive = this.AttachedData
                .Select((kv) => kv.Key.GetRemoteAddress() is Terraria.Net.TcpAddress tcpa ? tcpa.Address : null)
                .Where((a) => a != null)
                .Select(a => a!)
                .ToArray();
            var tbr = this.Connections
                .Where((kv) => !alive.Any(a => a.Equals(kv.Value.Address)))
                .Select((kv) => kv.Key)
                .ToList();
            foreach (var k in tbr)
            {
                this.Connections.TryRemove(k, out _);
            }
        }
    }

    internal class AttachedRawData
    {
        public double ConnectTime;
    }

    private readonly ConnectionStore _connPool = new ConnectionStore();
    private void MMHook_Mitigation_OnConnectionAccepted(On.Terraria.Netplay.orig_OnConnectionAccepted orig, Terraria.Net.Sockets.ISocket client)
    {
        var mitigation = this.config.Mitigation.Value;
        var cl = mitigation.ConnectionLimit.Value;
        var nl = mitigation.LimitedNetwork.Value;

        if (mitigation.DisableAllMitigation
            || cl.Count == 0 || client.GetRemoteAddress() is not Terraria.Net.TcpAddress tcpa
            || nl is Config.MitigationSettings.NetworkLimit.None
            || (nl is Config.MitigationSettings.NetworkLimit.Public && Utils.PrivateIPAddress(tcpa.Address)))
        {
            orig(client);
            return;
        }

        var addrs = tcpa.Address.ToString();
        var cd = this._connPool.Connections.GetOrAdd(addrs, _ => new ConnectionStore.Connection
        {
            Address = tcpa.Address,
            Limit = new ConcurrentBag<Limiter>(cl.Select(lc => (Limiter) lc)),
        });
        var time = new TimeSpan(DateTime.Now.Ticks).TotalSeconds;
        this._connPool.AttachedData.Add(client, new AttachedRawData
        {
            ConnectTime = time,
        });
        foreach (var limiter in cd.Limit)
        {
            if (!limiter.Allowed)
            {
                Interlocked.Increment(ref this.Statistics.MitigationRejectedConnection);
                client.Close();
                TShockAPI.TShock.Log.ConsoleInfo($"Connection from {tcpa.Address} ({tcpa.Port}) rejected due to connection limit.");
                return;
            }
        }
        this.CheckConnectionTimeout();

        orig(client);
    }

    private void CheckConnectionTimeout()
    {
        var count = Terraria.Netplay.Clients.Count(rc => rc?.IsConnected() == true);

        if (count <= Terraria.Main.maxNetPlayers * 0.6)
        {
            return;
        }

        for (var i = 0; i < Terraria.Main.maxNetPlayers; i++)
        {
            if (Terraria.Netplay.Clients[i].IsConnected()
                && Terraria.Netplay.Clients[i].Socket.GetRemoteAddress() is Terraria.Net.TcpAddress tcpa)
            {
                if (!this._connPool.AttachedData.TryGetValue(Terraria.Netplay.Clients[i].Socket, out var ct))
                {
                    throw new Exception("Connection time not found");
                }

                var time = new TimeSpan(DateTime.Now.Ticks).TotalSeconds;

                foreach (var (state, timeout) in this.config.Mitigation.Value.ConnectionStateTimeout.Value)
                {
                    var elapsed = time - ct.ConnectTime;
                    if (Terraria.Netplay.Clients[i].State == state && elapsed > timeout)
                    {
                        Interlocked.Increment(ref this.Statistics.MitigationTerminatedConnection);
                        Terraria.Netplay.Clients[i].Socket.Close();
                        TShockAPI.TShock.Log.ConsoleInfo($"Connection from {tcpa.Address} ({tcpa.Port}, state {state} for {Math.Round(time - ct.ConnectTime, 1):G}s) disconnected due to connection state timeout.");
                        break;
                    }
                }
            }
        }

        this._connPool.PurgeCache();
    }
}