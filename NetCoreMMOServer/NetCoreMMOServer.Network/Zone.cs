using MemoryPack;
using NetCoreMMOServer.Packet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetCoreMMOServer.Network
{
    public class Zone
    {
        private ushort _zoneID;
        private ZoneDataTable _zonePacket = new();

        private PacketBufferWriter _removeZoneDataBufferWriter = new(new byte[2048]);
        private PacketBufferWriter _addZoneDataBufferWriter = new(new byte[2048]);
        private PacketBufferWriter _updateZoneDataBufferWriter = new(new byte[2048]);

        private List<EntityDataBase> _oldEntities = new();
        private List<EntityDataBase> _currentEntities = new();
        private List<EntityDataBase> _addEntities = new();
        private List<EntityDataBase> _removeEntities = new();

        public List<EntityDataBase> OldEntities => _oldEntities;
        public List<EntityDataBase> CurrentEntities => _currentEntities;
        public List<EntityDataBase> AddEntities => _addEntities;
        public List<EntityDataBase> RemoveEntities => _removeEntities;

        public void Init(ushort zoneID)
        {
            _zoneID = zoneID;
            _zonePacket.ZoneInfo = (ZoneType.Remove, _zoneID);
            _removeZoneDataBufferWriter.Clear();
            _currentEntities.Clear();
            //write _removeZoneDataBufferWriter
        }

        /// <summary>
        /// Server Write Buffer and Send
        /// </summary>
        public void WriteBuffer()
        {
            // _addZoneDataBufferWriter
            _zonePacket.ZoneInfo = (ZoneType.Add, _zoneID);
            _zonePacket.EntityDataTableList.Clear();
            foreach (EntityDataBase entity in _currentEntities)
            {
                var dataTablePacket = entity.InitDataTablePacket();
                _zonePacket.EntityDataTableList.Add(dataTablePacket);
            }
            MemoryPackSerializer.Serialize(_addZoneDataBufferWriter, _zonePacket);

            // _updateZoneDataBufferWriter
            _zonePacket.ZoneInfo = (ZoneType.Update, _zoneID);
            _zonePacket.EntityDataTableList.Clear();
            foreach (EntityDataBase entity in _currentEntities)
            {
                var dataTablePacket = entity.UpdateDataTablePacket();
                _zonePacket.EntityDataTableList.Add(dataTablePacket);
            }
            MemoryPackSerializer.Serialize(_updateZoneDataBufferWriter, _zonePacket);
        }

        /// <summary>
        /// Client Receive and ReadBuffer
        /// </summary>
        public void ReadBuffer(ZoneDataTable zoneData)
        {
            if(zoneData.ZoneInfo.zoneID != _zoneID)
            {
                Console.WriteLine("Error:: Not Equals ZoneID");
                return;
            }


        }

        public void AddEntity(EntityDataBase entity)
        {
            if(_currentEntities.Contains(entity))
            {
                return;
            }
            _currentEntities.Add(entity);
            _addEntities.Add(entity);
        }

        public void RemoveEntity(EntityDataBase entity)
        {
            if(!_currentEntities.Contains(entity))
            {
                return;
            }
            _currentEntities.Remove(entity);
            _removeEntities.Add(entity);
        }

        public void ResetAndBackupEntityList()
        {
            _addEntities.Clear();
            _removeEntities.Clear();
            _oldEntities.Clear();

            foreach(var entity in _currentEntities)
            {
                _oldEntities.Add(entity);
            }
        }
    }
}
