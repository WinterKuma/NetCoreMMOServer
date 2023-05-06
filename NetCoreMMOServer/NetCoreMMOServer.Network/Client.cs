using NetCoreMMOServer.Packet;
using NetCoreMMOServer.Utility;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;

namespace NetCoreMMOServer.Network
{
    public class Client
    {
        [AllowNull] private Socket _clientSocket;
        private IDuplexPipe? _pipe;

        public event AsyncAction<Socket>? Connected;
        public event Action<MPacket>? Received;

        public Client()
        {
        }

        public void OnConnect(IPEndPoint serverEP)
        {
            _clientSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            //_clientSocket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.ReuseAddress, true);
            _clientSocket.ConnectAsync(serverEP).Wait();

            _ = ConnectAsync(_clientSocket);
        }

        public async Task OnConnectAsync(IPEndPoint serverEP)
        {
            _clientSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            //_clientSocket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.ReuseAddress, true);
            await _clientSocket.ConnectAsync(serverEP);

            _ = ConnectAsync(_clientSocket);
        }

        private async Task ConnectAsync(Socket socket)
        {
            Console.WriteLine($"[{socket.RemoteEndPoint}]: connected");

            _pipe = new DuplexPipe(new NetworkStream(socket));
            Connected?.Invoke(socket);

            await Task.WhenAll(ReceiveAsync());

            await _pipe.Input.CompleteAsync();
            await _pipe.Output.CompleteAsync();

            Console.WriteLine($"[{socket.RemoteEndPoint}]: disconnected");
        }

        public async Task SendAsync(ReadOnlyMemory<byte> buffer)
        {
            if (_pipe is null)
            {
                return;
            }

            FlushResult result = await _pipe.Output.WriteAsync(buffer);
            if (result.IsCompleted)
            {
                Console.WriteLine($"SendAsync Result IsCompleted");
            }
        }

        private async Task ReceiveAsync()
        {
            while (true)
            {
                if (_pipe is null)
                {
                    break;
                }

                ReadResult result = await _pipe.Input.ReadAsync();
                ReadOnlySequence<byte> buffer = result.Buffer;
                MPacket packet = new();

                while (BufferResolver.TryReadPacket(ref buffer, ref packet))
                {
                    Received?.Invoke(packet);
                }

                _pipe.Input.AdvanceTo(buffer.Start, buffer.End);

                if (result.IsCompleted)
                {
                    break;
                }
            }
        }

        public void Disconnect()
        {
            // Disconnect sockets
            _clientSocket.Shutdown(SocketShutdown.Both);
            _clientSocket.Disconnect(true);
            _clientSocket.Close();

            // Clear pipelines
            _pipe?.Input.Complete();
            _pipe?.Output.Complete();
        }
    }
}
