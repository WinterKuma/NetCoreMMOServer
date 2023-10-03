using NetCoreMMOServer.Packet;
using NetCoreMMOServer.Utility;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;

namespace NetCoreMMOServer.Network
{
    public class Client : PipeSocket
    {
        public event AsyncAction<Socket>? Connected;

        public Client()
        {
            //Socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.ReuseAddress, true);
        }

        public void OnConnect(IPEndPoint serverEP)
        {
            Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            socket.ConnectAsync(serverEP).Wait();

            _ = ConnectAsync(socket);
        }

        public async Task OnConnectAsync(IPEndPoint serverEP)
        {
            Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            await socket.ConnectAsync(serverEP);

            _ = ConnectAsync(socket);
        }

        private async Task ConnectAsync(Socket socket)
        {
            Console.WriteLine($"[{socket.RemoteEndPoint}]: connected");
            SetSocket(socket);

            Connected?.Invoke(socket);

            await Task.WhenAll(ReceiveAsync());

            await Reader.CompleteAsync();
            await Writer.CompleteAsync();

            Console.WriteLine($"[{socket.RemoteEndPoint}]: disconnected");
        }

        public void Disconnect()
        {
            Disconnect(false);
        }
    }
}
