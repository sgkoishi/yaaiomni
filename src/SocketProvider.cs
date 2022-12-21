using Terraria.Net.Sockets;
using TerrariaApi.Server;
using TShockAPI.Sockets;

namespace Chireiden.TShock.Omni;

public partial class Plugin : TerrariaPlugin
{
    /// <summary>
    /// We found memory leak, from the memory dump it seems that the async networking is using much more memory than expected.
    /// <code>
    /// <seealso cref="System.Threading.ThreadPool.s_workQueue"/>,
    /// -> <seealso cref="System.Net.Sockets.SocketAsyncContext+BufferMemorySendOperation"/>,
    ///   -> <seealso cref="System.Action.{System.Int32, System.Byte[], System.Int32, System.Net.Sockets.SocketFlags, System.Net.Sockets.SocketError}"/>,
    ///     -> <seealso cref="System.Net.Sockets.Socket.AwaitableSocketAsyncEventArgs"/>,
    /// -> <seealso cref="System.Threading.QueueUserWorkItemCallbackDefaultContext"/>,
    ///   -> <seealso cref="System.Net.Sockets.SocketAsyncContext+BufferMemorySendOperation"/>,
    ///     -> <seealso cref="System.Action.{System.Int32, System.Byte[], System.Int32, System.Net.Sockets.SocketFlags, System.Net.Sockets.SocketError}"/>,
    ///       -> <seealso cref="System.Net.Sockets.Socket.AwaitableSocketAsyncEventArgs"/>
    /// </code>
    /// 
    /// This class uses blocked impl instead of async.
    /// </summary>
    public class HackyBlockedSocket : LinuxTcpSocket, ISocket
    {
        void ISocket.AsyncSend(byte[] data, int offset, int size, SocketSendCallback callback, object state)
        {
            callback(state);
            this._connection.GetStream().Write(data, offset, size);
        }
    }
    public class HackyAsyncSocket : LinuxTcpSocket, ISocket
    {
        void ISocket.AsyncSend(byte[] data, int offset, int size, SocketSendCallback callback, object state)
        {
            callback(state);
            this._connection.GetStream().WriteAsync(data, offset, size);
        }
    }
    public class AnotherAsyncSocket : LinuxTcpSocket, ISocket
    {
        async void ISocket.AsyncSend(byte[] data, int offset, int size, SocketSendCallback callback, object state)
        {
            await this._connection.GetStream().WriteAsync(data.AsMemory(offset, size));
            callback(state);
        }
    }
}
