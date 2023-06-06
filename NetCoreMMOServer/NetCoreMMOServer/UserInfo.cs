using MemoryPack;
using NetCoreMMOServer.Network;
using NetCoreMMOServer.Packet;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace NetCoreMMOServer
{
    internal class UserInfo
    {
        /// <summary>
        /// UserInfo Init Value
        /// </summary>
        private static int s_MaxID = 0;
        private readonly int _id;

        [AllowNull]
        private User _user;

        /// <summary>
        /// Contents Value
        /// </summary>
        private Vector3 _position;
        //private List<SyncData> _syncDatas;
        //private MovementSD _movementSD;

        /// <summary>
        /// Packet and Zone System Value
        /// </summary>
        private PacketBufferWriter _packetBufferWriter;
        private HashSet<Zone> _zones;
        private HashSet<Zone> _addZones;
        private HashSet<Zone> _removeZones;

        public UserInfo()
        {
            _user = null;
            _id = ++s_MaxID;

            //_syncDatas = new()
            //{
            //    (_movementSD = new MovementSD())
            //};

            _packetBufferWriter = new PacketBufferWriter(new byte[2048]);
        }

        public int Id => _id;
        public User User
        {
            get => _user;
            set => _user = value;
        }
        //public Vector3 Position
        //{
        //    //get => _movementSD.Position.Value;
        //    //set => _movementSD.Position.Value = value;
        //}

        public PacketBufferWriter PacketBufferWriter => _packetBufferWriter;
        public HashSet<Zone> Zones => _zones;
        public HashSet<Zone> AddZones => _addZones;
        public HashSet<Zone> RemoveZones => _removeZones;

    //    public void WritePacket()
    //    {
    //        _packetBufferWriter.Clear();

    //        foreach (var zone in RemoveZones)
    //        {
    //            if(Zones.Remove(zone))
    //            {

    //            }
    //        }

    //        foreach (var zone in Zones)
    //        {
    //            // Add ZoneID
    //            foreach(var userInfo in zone.UserList)
    //            {
    //                // Add UserID
    //                foreach(var syncData in userInfo._syncDatas)
    //                {
    //                    // Add SyncDataID?
    //                    if (syncData.IsDirty)
    //                    {
    //                        MemoryPackSerializer.Serialize<IMPacket, PacketBufferWriter>(_packetBufferWriter, syncData);
    //                    }
    //                }
    //            }
    //        }

    //        // 추가된 Zone, DirtyFlag에 관계 없이 데이터 추가
    //        foreach (var zone in AddZones)
    //        {
    //            if(Zones.Add(zone))
    //            {
    //                foreach (var userInfo in zone.UserList)
    //                {
    //                    foreach(var syncData in userInfo._syncDatas)
    //                    {
    //                        MemoryPackSerializer.Serialize<IMPacket, PacketBufferWriter>(_packetBufferWriter, syncData);
    //                    }
    //                }
    //            }
    //        }
    //    }
    }
}
