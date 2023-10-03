using System.IO.Pipelines;
using System.Net.Sockets;

namespace NetCoreMMOServer.Network
{
    public class DuplexPipe : IDuplexPipe, IDisposable, IAsyncDisposable
    {
        private NetworkStream _stream;
        private PipeReader _reader;
        private PipeWriter _writer;

        public DuplexPipe(NetworkStream stream)
        {
            _stream = stream;
            _reader = PipeReader.Create(stream);
            _writer = PipeWriter.Create(stream);
        }

        ~DuplexPipe() => Dispose();

        public PipeReader Input => _reader;
        public PipeWriter Output => _writer;

        public void Dispose()
        {
            _reader.Complete();
            _writer.Complete();
            _stream.Dispose();
        }

        public ValueTask DisposeAsync()
        {
            return _stream.DisposeAsync();
        }
    }
}
