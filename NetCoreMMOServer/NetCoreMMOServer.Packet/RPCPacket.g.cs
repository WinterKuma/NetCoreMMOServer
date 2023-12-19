using MemoryPack;
using System.Numerics;

namespace NetCoreMMOServer.Packet
{
    [MemoryPackUnion(1, typeof(RPCAttackPacket))]
    [MemoryPackUnion(2, typeof(RPCTestPacket))]
    [MemoryPackUnion(3, typeof(RPCGetDamagePacket))]
    [MemoryPackUnion(4, typeof(RPCTest3Packet))]
    public abstract partial class RPCPacket
    {
        
    }

    [MemoryPackable]
    public partial class RPCAttackPacket : RPCPacket
    {
    }

    [MemoryPackable]
    public partial class RPCTestPacket : RPCPacket
    {
        public Vector3 value { get; set; } = default;
    }

    [MemoryPackable]
    public partial class RPCGetDamagePacket : RPCPacket
    {
        public int damage { get; set; } = default;
    }

    [MemoryPackable]
    public partial class RPCTest3Packet : RPCPacket
    {
        public int val1 { get; set; } = default;
        public float val2 { get; set; } = default;
    }
}
