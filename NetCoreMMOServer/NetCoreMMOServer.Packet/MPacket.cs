using MemoryPack;
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
    [MemoryPackUnion(1, typeof(EntityDataTable))]
    [MemoryPackUnion(2, typeof(ZoneDataTable))]
    [MemoryPackUnion(3, typeof(SetLinkedEntityPacket))]
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
        //            value = PacketDtoPoolProvider.GetDtoPool<EntityDto>().GetDto();
        //            break;
        //        case 1:
        //            value = PacketDtoPoolProvider.GetDtoPool<MoveDto>().GetDto();
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

    [MemoryPackable]
    public partial class SyncData<T> : ISyncData
    {
        private T _value;
        private bool _dirtyFlag;

        public SyncData(T value = default)
        {
            _value = value;
            _dirtyFlag = false;
        }

        public T Value
        {
            get { return _value; }
            set
            {
                if (!EqualityComparer<T>.Default.Equals(_value, value))
                {
                    _value = value;
                    _dirtyFlag = true;
                }
            }
        }

        public bool IsDirty
        {
            get { return _dirtyFlag; }
            set { _dirtyFlag = value; }
        }
    }

    [MemoryPackable]
    [MemoryPackUnion(14, typeof(SyncData<bool>))]
    [MemoryPackUnion(1, typeof(SyncData<byte>))]
    [MemoryPackUnion(2, typeof(SyncData<short>))]
    [MemoryPackUnion(3, typeof(SyncData<ushort>))]
    [MemoryPackUnion(4, typeof(SyncData<int>))]
    [MemoryPackUnion(5, typeof(SyncData<uint>))]
    [MemoryPackUnion(6, typeof(SyncData<long>))]
    [MemoryPackUnion(7, typeof(SyncData<ulong>))]
    [MemoryPackUnion(8, typeof(SyncData<float>))]
    [MemoryPackUnion(9, typeof(SyncData<double>))]
    [MemoryPackUnion(10, typeof(SyncData<Vector2>))]
    [MemoryPackUnion(11, typeof(SyncData<Vector3>))]
    [MemoryPackUnion(12, typeof(SyncData<Vector4>))]
    [MemoryPackUnion(13, typeof(SyncData<Quaternion>))]
    public partial interface ISyncData
    {
        public bool IsDirty { get; set; }
    }

    public enum EntityType : byte
    {
        None = 0,
        Player = 1,
    }

    public struct EntityInfo
    {
        public EntityInfo()
        {
        }

        public EntityType EntityType { get; set; } = EntityType.None;
        public uint EntityID { get; set; } = 0;
    }

    [MemoryPackable]
    public partial class EntityDataTable : IMPacket
    {
        [MemoryPackIgnore]
        public bool IsCashed { get; set; } = false;
        public EntityInfo EntityInfo { get; set; } = default;
        public Dictionary<byte, ISyncData> DataTable { get; set; } = new(32);
    }

    public enum ZoneType : byte
    {
        None = 0,
        Add,
        Update,
        Remove,
    }

    [MemoryPackable]
    public partial class ZoneDataTable : IMPacket
    {
        public (ZoneType zoneType, ushort zoneID) ZoneInfo { get; set; } = (ZoneType.None, 0);
        public List<EntityDataTable> EntityDataTableList { get; set; } = new(1024);
    }

    [MemoryPackable]
    public partial class SetLinkedEntityPacket : IMPacket
    {
        public EntityInfo EntityInfo { get; set; }
    }
}