using System;
using MemoryPack;
using NetCoreMMOServer.Utility;

namespace NetCoreMMOServer.Packet
{
    public static partial class PacketExtensions
    {
        private static ConcurrentPool<PacketBufferWriter> _packetBufferWriterPool = new();

        public static PacketProtocol GetProtocol(this Dto dto, Type type)
        {
            if (DtoPacketProtocolDictionary.TryGetValue(type, out PacketProtocol packetProtocol))
            {
                return packetProtocol;
            }
            return PacketProtocol.None;
        }

        public static ReadOnlyMemory<byte> AsMemory(this MPacket packet)
        {
            PacketBufferWriter writer = _packetBufferWriterPool.Get();
            writer.Clear();
            MemoryPackSerializer.Serialize(writer, packet);

            ReadOnlyMemory<byte> result = writer.GetFilledBuffer();

            _packetBufferWriterPool.Return(writer);
            return result;
        }

        public static void ToMPacket<T>(this T dto, ref MPacket packet) where T : Dto
        {
            PacketBufferWriter writer = _packetBufferWriterPool.Get();
            writer.Clear();
            MemoryPackSerializer.Serialize(writer, dto);

            packet.PacketProtocol = dto.GetProtocol(typeof(T));
            packet.Dto = writer.GetFilledBuffer();

            _packetBufferWriterPool.Return(writer);
        }

        public static MPacket ToMPacket<T>(this T dto) where T : Dto
        {
            PacketBufferWriter writer = _packetBufferWriterPool.Get();
            writer.Clear();
            MemoryPackSerializer.Serialize(writer, dto);

            MPacket packet = new()
            {
                PacketProtocol = dto.GetProtocol(typeof(T)),
                Dto = writer.GetFilledBuffer()
            };

            _packetBufferWriterPool.Return(writer);
            return packet;
        }

        public static T? Deserialize<T>(this MPacket packet)
        {
            return MemoryPackSerializer.Deserialize<T>(packet.Dto.Span);
        }

        public static int Deserialize<T>(this MPacket packet, ref T? dto)
        {
            return MemoryPackSerializer.Deserialize<T>(packet.Dto.Span, ref dto);
        }

        public static Dto? Deserialize(this MPacket packet)
        {
            return DeserializeDictionary[packet.PacketProtocol].Invoke(packet);
        }
    }
}
