using MemoryPack;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace NetCoreMMOServer.Packet
{
    public struct PacketBufferWriter : IBufferWriter<byte>
    {
        private byte[] _buffer;
        private int _written;
        private int _consumed;

        public PacketBufferWriter()
        {
            _buffer = new byte[1024];
            _written = 0;
            _consumed = 0;
        }

        public PacketBufferWriter(byte[] buffer)
        {
            _buffer = buffer;
            _written = 0;
            _consumed = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Advance(int count)
        {
            _written += count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            for (int i = 0; i < _written; i++)
            {
                _buffer[i] = 0;
            }
            _written = 0;
            _consumed = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            Memory<byte> result = _buffer.AsMemory(_written);
            if (result.Length >= sizeHint)
            {
                return result;
            }

            MemoryPackSerializationException.ThrowMessage("Requested invalid sizeHint.");
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<byte> GetSpan(int sizeHint = 0)
        {
            Span<byte> result = _buffer.AsSpan(_written);
            if (result.Length >= sizeHint)
            {
                return result;
            }

            MemoryPackSerializationException.ThrowMessage("Requested invalid sizeHint.");
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlyMemory<byte> GetFilledMemory(int sizeHint = 0)
        {
            ReadOnlyMemory<byte> result = _buffer.AsMemory(_consumed, _written - _consumed);
            if (result.Length >= sizeHint)
            {
                _consumed = _written;
                return result;
            }

            MemoryPackSerializationException.ThrowMessage("Requested invalid sizeHint.");
            return result;
        }
    }
}
