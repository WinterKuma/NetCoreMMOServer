using NetCoreMMOServer.Utility;
using System.Net;
using System.Net.Sockets;

namespace NetCoreMMOServer.Network
{
    public abstract class Server
    {
        private readonly int _port;
        private readonly TcpListener _listener;

        //public event AsyncPredicate<Socket>? Accepting;
        //public event AsyncAction<Socket>? Accepted;

        public Server(int port)
        {
            _port = port;
            _listener = new TcpListener(IPAddress.Any, _port);
        }

        public int Port => _port;
        public TcpListener ServerSocket => _listener;

        public void Start(int backlog = (int)SocketOptionName.MaxConnections)
        {
            //_listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start(backlog);

            _ = OnAccept().ConfigureAwait(false);
        }

        protected abstract Task Accepted(Socket socket);
        protected abstract Task<bool> Accepting(Socket socket);


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
                    _ = AcceptAsync(socket).ConfigureAwait(false);
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
            possibleAccept = await Accepting(socket).ConfigureAwait(false);

            if (possibleAccept)
            {
                //Not Used Nagle Algorithm
                socket.NoDelay = true;

                await Accepted(socket).ConfigureAwait(false);
            }
            else
            {
                socket.Dispose();
            }
        }
    }
}