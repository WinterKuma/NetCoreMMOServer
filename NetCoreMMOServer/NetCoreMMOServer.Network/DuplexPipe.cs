using System.IO.Pipelines;
using System.Net.Sockets;

namespace NetCoreMMOServer.Network
{
    public class DuplexPipe : IDuplexPipe
    {
        private PipeReader _reader;
        private PipeWriter _writer;

        public DuplexPipe(NetworkStream stream)
        {
            _reader = PipeReader.Create(stream);
            _writer = PipeWriter.Create(stream);
        }

        public PipeReader Input => _reader;
        public PipeWriter Output => _writer;
    }
}
