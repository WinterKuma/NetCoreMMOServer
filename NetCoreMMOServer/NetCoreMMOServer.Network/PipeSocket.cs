using NetCoreMMOServer.Packet;
using System.Buffers;
using System.IO.Pipelines;
using System.Net.Sockets;

namespace NetCoreMMOServer.Network
{
    public class PipeSocket : IDisposable
    {
        private Socket _socket;
        private DuplexPipe _pipe;

        public PipeSocket()
        {
            _socket = null!;
            _pipe = null!;
        }

        public PipeSocket(Socket socket)
        {
            _socket = socket;
            _pipe = new DuplexPipe(new NetworkStream(socket));
        }

        public void SetSocket(Socket socket)
        {
            _socket = socket;
            _pipe = new DuplexPipe(new NetworkStream(socket));
        }

        public void SetSocket(Socket socket, DuplexPipe pipe)
        {
            _socket = socket;
            _pipe = pipe;
        }

        ~PipeSocket() => Dispose();

        public void Dispose()
        {
            _socket.Close();
            _pipe.Dispose();
        }

        public void Disconnect(bool reuseSocket = true)
        {
            if (reuseSocket)
            {
                _socket.Disconnect(reuseSocket);
            }
            else
            {
                Dispose();
            }
        }

        public Socket Socket => _socket;
        public DuplexPipe Pipe => _pipe;
        public PipeReader Reader => _pipe.Input;
        public PipeWriter Writer => _pipe.Output;
        public Action<IMPacket, PipeSocket>? Received;

        public async virtual Task SendAsync(ReadOnlyMemory<byte> buffer)
        {
            if (Pipe is null)
            {
                return;
            }

            FlushResult result = await Writer.WriteAsync(buffer).ConfigureAwait(false);
            if (result.IsCompleted)
            {
                Console.WriteLine($"SendAsync Result IsCompleted");
            }
        }

        public async virtual Task ReceiveAsync()
        {
            try
            {
                while (true)
                {
                    if (Pipe is null)
                    {
                        break;
                    }

                    ReadResult result = await Reader.ReadAsync().ConfigureAwait(false);
                    ReadOnlySequence<byte> buffer = result.Buffer;

                    while (BufferResolver.TryReadPacket(ref buffer, out var packet))
                    {
                        if (packet == null) continue;

                        Received?.Invoke(packet, this);
                    }

                    Reader.AdvanceTo(buffer.Start, buffer.End);

                    if (result.IsCompleted)
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Disconnect Socket[{Socket}] Error[{ex}]");
            }
        }
    }
}
