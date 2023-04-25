using System.Buffers;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Net;
using NetCoreMMOServer.Utility;

namespace NetCoreMMOServer.Network
{
    public class Server
    {
        private readonly int _port;
        private TcpListener? _listener;

        public event AsyncPredicate<Socket>? Accepting;
        public event AsyncAction<Socket>? Accepted;

        public Server(int port)
        {
            _port = port;
        }

        public void Start(int backlog = (int)SocketOptionName.MaxConnections)
        {
            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start(backlog);

            _ = OnAccept().ConfigureAwait(false);
        }

        private async Task OnAccept()
        {
            try
            {
                while (true)
                {
                    if (_listener is null)
                    {
                        return;
                    }

                    var socket = await _listener.AcceptSocketAsync().ConfigureAwait(false);
                    _ = AcceptAsync(socket);
                }
            }
            catch (Exception ex)
            {
                //TODO : Console.WriteLine -> C# ILogger
                Console.WriteLine(ex.ToString());
                Console.WriteLine($"Error::OnAccept Server Stop!!!");
                return;
            }
        }

        private async Task AcceptAsync(Socket socket)
        {
            bool possibleAccept = true;
            if (Accepting is not null)
            {
                possibleAccept = await Accepting.Invoke(socket).ConfigureAwait(false);
            }

            if (possibleAccept)
            {
                //Not Used Nagle Algorithm
                socket.NoDelay = true;

                if (Accepted is not null)
                {
                    await Accepted.Invoke(socket).ConfigureAwait(false);
                }
            }
            else
            {
                socket.Dispose();
            }
        }
    }
}