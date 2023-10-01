using MemoryPack;
using NetCoreMMOServer.Packet;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NetCoreMMOServer.Network
{
    public class BufferResolver
    {
        public static bool TryReadPacket(ref ReadOnlySequence<byte> buffer, out IMPacket? packet)
        {
            packet = null;

            if (buffer.Length <= 0) return false;

            if (!TryReadUnionHeader(buffer, out ushort tag))
            {
                Console.WriteLine($"Error:: Can't Read Union Header!!!");
                return false;
            }

            int length = 0;
            try
            {
                switch (tag)
                {
                    case 1:
                        packet = PacketPool.Get<EntityDataTable>();
                        break;
                    case 2:
                        packet = PacketPool.Get<SetLinkedEntityPacket>();
                        break;
                    case 3:
                        packet = PacketPool.Get<GroundModificationPacket>();
                        break;

                    default:
                        return false;
                }

                length = MemoryPackSerializer.Deserialize(buffer, ref packet);
            }
            catch
            {
                //Console.WriteLine($"Error:: MemoryPackSerializer.Deserialize() throw {ex}");
                return false;
            }
            if (length == 0)
            {
                return false;
            }

            buffer = buffer.Slice(length);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryReadUnionHeader(in ReadOnlySequence<byte> buffer, out ushort tag)
        {
            byte spanReference = Unsafe.As<byte, byte>(ref MemoryMarshal.GetReference(buffer.FirstSpan)); ;
            if (spanReference < 250)
            {
                tag = spanReference;
                return true;
            }

            if (spanReference == 250)
            {
                tag = Unsafe.ReadUnaligned<ushort>(ref Unsafe.As<byte, byte>(ref MemoryMarshal.GetReference(buffer.FirstSpan)));
                return true;
            }

            tag = 0;
            return false;
        }
    }
}
