using System.IO.Pipelines;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace NetCoreMMOServer.Network
{
    public class User
    {
        private Socket _socket;
        private IDuplexPipe _pipe;

        public User(Socket socket, IDuplexPipe pipe)
        {
            _socket = socket;
            _pipe = pipe;
        }

        public User(Socket socket)
        {
            _socket = socket;
            _pipe = new DuplexPipe(new NetworkStream(socket));
        }

        public Socket Socket => _socket;
        public IDuplexPipe Pipe => _pipe;
        public PipeReader Reader => _pipe.Input;
        public PipeWriter Writer => _pipe.Output;

        public async Task CompleteAsync()
        {
            await _pipe.Input.CompleteAsync();
            await _pipe.Output.CompleteAsync();
        }
    }
}
