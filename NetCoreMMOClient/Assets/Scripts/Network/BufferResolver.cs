using MemoryPack;
using NetCoreMMOServer.Packet;
using System;
using System.Buffers;
using System.Diagnostics;

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

        public static bool TryReadPacket(ref ReadOnlySequence<byte> buffer, out IMPacket? packet)
        {
            packet = null;

            if (buffer.Length <= 0) return false;


            int length = 0;
            try
            {
                length = MemoryPackSerializer.Deserialize(buffer, ref packet);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error:: MemoryPackSerializer.Deserialize() throw {ex}");
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
