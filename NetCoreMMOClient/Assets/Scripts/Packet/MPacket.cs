using MemoryPack;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using UnityEngine;

namespace NetCoreMMOServer.Packet
{
    [MemoryPackable]
    [MemoryPackUnion(1, typeof(EntityDataTable))]
    [MemoryPackUnion(2, typeof(SetLinkedEntityPacket))]
    [MemoryPackUnion(3, typeof(GroundModificationPacket))]
    [MemoryPackUnion(4, typeof(RPCPacketProtocol))]
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
            //Debug.Assert(syncData is SyncData<T>);
            if(syncData is SyncData<T> data)
            {
                Value = data.Value;
                _dirtyFlag = true;
            }
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
    [MemoryPackUnion(10, typeof(SyncData<UnityEngine.Vector2>))]
    [MemoryPackUnion(11, typeof(SyncData<UnityEngine.Vector3>))]
    [MemoryPackUnion(12, typeof(SyncData<UnityEngine.Vector4>))]
    [MemoryPackUnion(13, typeof(SyncData<UnityEngine.Quaternion>))]
    public partial interface ISyncData
    {
        public bool IsDirty { get; set; }
        public void SetValue(ISyncData syncData);
    }

    public enum EntityType : byte
    {
        None = 0,
        Player = 1,
        Block = 2,
    }

    public struct EntityInfo : IEquatable<EntityInfo>
    {
        public EntityType EntityType { get; set; }
        public uint EntityID { get; set; }

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
        public IRPCPacket RPCPacket { get; set; }
    }


    [MemoryPackable]
    [MemoryPackUnion(1, typeof(RPCAttackPacket))]
    [MemoryPackUnion(2, typeof(RPCTestPacket))]
    public partial interface IRPCPacket
    {

    }

    [MemoryPackable]
    public partial class RPCAttackPacket : IRPCPacket
    {

    }

    [MemoryPackable]
    public partial class RPCTestPacket : IRPCPacket
    {
        public UnityEngine.Vector3 value { get; set; } = UnityEngine.Vector3.zero;
    }
}