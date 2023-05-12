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
    public partial class EntityDto : NetObject
    {
        public bool IsMine { get; set; } = false;
        public bool IsSpawn { get; set; } = false;
        public Vector3 Position { get; set; }
    }

    [Packetable]
    [MemoryPackable]
    public partial class MoveDto : NetObject
    {
        public Vector3 Position { get; set; }
    }
}