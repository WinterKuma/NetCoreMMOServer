using MemoryPack;
using NetCoreMMOServer.Utility;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace NetCoreMMOServer.Packet
{
    [MemoryPackable]
    [MemoryPackUnion(1, typeof(EntityDataTable))]
    [MemoryPackUnion(2, typeof(SetLinkedEntityPacket))]
    [MemoryPackUnion(3, typeof(GroundModificationPacket))]
    [MemoryPackUnion(4, typeof(RPCPacketProtocol))]
    public partial interface IMPacket
    {

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

        public void SetValue(ISyncData syncData)
        {
            if (syncData is SyncData<T> data)
            {
                ForceSetValue(data.Value);
            }
        }

        public void ForceSetValue(T value)
        {
            _value = value;
            _dirtyFlag = true;
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

        public void SetValue(ISyncData syncData);
    }

    public enum BlockType : byte
    {
        None = 0,
        Block = 1,
    }

    [MemoryPackable]
    public partial class ZoneChunk
    {
        public BlockType[,,] chunks = new BlockType[3, 3, 3];
    }

    public enum EntityType : byte
    {
        None = 0,
        Player = 1,
        Block = 2,
    }

    public struct EntityInfo : IEquatable<EntityInfo>
    {
        public EntityInfo()
        {
        }

        public EntityInfo(uint entityId, EntityType entityType)
        {
            EntityID = entityId;
            EntityType = entityType;
        }

        public EntityType EntityType { get; set; } = EntityType.None;
        public uint EntityID { get; set; } = 0;

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is EntityInfo info)
            {
                return this.EntityType == info.EntityType && this.EntityID == info.EntityID;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return EntityType.GetHashCode() ^ EntityID.GetHashCode();
        }

        public bool Equals(EntityInfo other)
        {
            return this.EntityType == other.EntityType && this.EntityID == other.EntityID;
        }

        public static bool operator ==(EntityInfo lhs, EntityInfo rhs)
        {
            return lhs.EntityType == rhs.EntityType && lhs.EntityID == rhs.EntityID;
        }

        public static bool operator !=(EntityInfo lhs, EntityInfo rhs)
        {
            return lhs.EntityType != rhs.EntityType || lhs.EntityID != rhs.EntityID;
        }
    }

    [MemoryPackable]
    public partial class EntityDataTable : IMPacket
    {
        [MemoryPackIgnore]
        public bool IsCashed { get; set; } = false;
        public EntityInfo EntityInfo { get; set; } = default;
        public Dictionary<byte, ISyncData> DataTable { get; set; } = new(32);
    }

    [MemoryPackable]
    public partial class RPCData
    {
        public EntityInfo EntityInfo { get; set; } = default;
        public byte Id { get; set; } = 0;
        public List<ISyncData> datas { get; set; } = new(12);
    }

    [MemoryPackable]
    public partial class SetLinkedEntityPacket : IMPacket
    {
        public EntityInfo EntityInfo { get; set; }
    }

    [MemoryPackable]
    public partial class GroundModificationPacket : IMPacket
    {
        public Vector3Int Position { get; set; }
        public bool IsCreate { get; set; }
    }


    [MemoryPackable]
    public partial class RPCPacketProtocol : IMPacket
    {
        public RPCPacket RPCPacket { get; set; }
    }

    [MemoryPackable]
    public abstract partial class RPCPacket
    {

    }
}