using MemoryPack;
using NetCoreMMOServer.Packet;
using System.Buffers;

namespace NetCoreMMOServer.Network
{
    public class BufferResolver
    {
        public static bool TryReadPacket(ref ReadOnlySequence<byte> buffer, ref MPacket packet)
        {
            if (buffer.Length <= 0) return false;

            int length = 0;
            try
            {
                length = MemoryPackSerializer.Deserialize(buffer, ref packet);
            }
            catch
            {
                return false;
            }
            if (length == 0)
            {
                return false;
            }

            buffer = buffer.Slice(length);
            return true;
        }
    }
}
