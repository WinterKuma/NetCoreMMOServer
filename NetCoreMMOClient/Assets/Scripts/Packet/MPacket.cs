using MemoryPack;
using System;
using System.Numerics;

namespace NetCoreMMOServer.Packet
{
    [MemoryPackable]
    public partial struct MPacket
    {
        [MemoryPackAllowSerialize]
        public PacketProtocol PacketProtocol { get; set; }
        public ReadOnlyMemory<byte> Dto { get; set; }
    }

    [MemoryPackable]
    [MemoryPackUnion(0, typeof(EntityDto))]
    [MemoryPackUnion(1, typeof(MoveDto))]
    public partial interface IMPacket
    {
        //[MemoryPackOnDeserializing]
        //static void ReadMPacket(ref MemoryPackReader reader, ref IMPacket? value)
        //{
        //    if (!reader.TryReadUnionHeader(out var tag))
        //    {
        //        value = default;

        //        return;
        //    }

        //    switch (tag)
        //    {
        //        case 0:
        //            value = new EntityDto();
        //            break;
        //        case 1:
        //            value = new MoveDto();
        //            break;
        //    }
        //}
    }

    public partial class Dto
    {
    }

    [MemoryPackable]
    public partial class NetObject : Dto
    {
        public int NetObjectID { get; set; }
    }

    [Packetable]
    [MemoryPackable]
    public partial class EntityDto : NetObject, IMPacket
    {
        public bool IsMine { get; set; } = false;
        public bool IsSpawn { get; set; } = false;
        public Vector3 Position { get; set; }
    }

    [Packetable]
    [MemoryPackable]
    public partial class MoveDto : NetObject, IMPacket
    {
        public Vector3 Position { get; set; }
    }
}