﻿using MemoryPack;
using NetCoreMMOServer.Packet;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net.Sockets;

namespace NetCoreMMOServer.Network
{
    public class User
    {
        private static int s_MaxID = 0;
        private readonly int _id;

        private Socket _socket;
        private IDuplexPipe _pipe;
        private EntityDataBase? _linkedEntity;

        private PacketBufferWriter _packetBufferWriter;
        private HashSet<Zone> _currentZones;
        private HashSet<Zone> _addZones;
        private HashSet<Zone> _removeZones;

        private List<EntityDataBase> _updateEntityList;
        private List<EntityDataBase> _initEntityList;
        private List<EntityDataBase> _disposeEntityList;

        public User()
        {
            _id = ++s_MaxID;

            _socket = null;
            _pipe = null;
            _linkedEntity = null;

            _packetBufferWriter = new(new byte[0xffff]);
            _currentZones = new();
            _addZones = new();
            _removeZones = new();

            _updateEntityList = new();
            _initEntityList = new();
            _disposeEntityList = new();
        }

        public User(int id, Socket? socket = null, IDuplexPipe? pipe = null) : this()
        {
            _id = id;

            if(socket != null)
            {
                _socket = socket;
                if(pipe == null)
                {
                    _pipe = new DuplexPipe(new NetworkStream(socket));
                }
            }

            if(pipe != null)
            {
                _pipe = pipe;
            }
        }

        public void Init(Socket socket)
        {
            _socket = socket;
            _pipe = new DuplexPipe(new NetworkStream(socket));

            _currentZones.Clear();
            _addZones.Clear();
            _removeZones.Clear();
            _updateEntityList.Clear();
            _initEntityList.Clear();
            _disposeEntityList.Clear();
        }

        public int ID => _id;
        public Socket Socket => _socket;
        public IDuplexPipe Pipe => _pipe;
        public PipeReader Reader => _pipe.Input;
        public PipeWriter Writer => _pipe.Output;
        public EntityDataBase? LinkedEntity => _linkedEntity;
        public ref PacketBufferWriter PacketBufferWriter => ref _packetBufferWriter;

        public async Task CompleteAsync()
        {
            await _pipe.Input.CompleteAsync();
            await _pipe.Output.CompleteAsync();
        }

        public void LinkEntity(EntityDataBase linkedEntity)
        {
            _linkedEntity = linkedEntity;
        }

        public void ClearWriter()
        {
            _packetBufferWriter.Clear();
        }

        public void WritePacket()
        {
            _addZones.Clear();
            _removeZones.Clear();
            if(_linkedEntity?.CurrentZone.IsDirty ?? false)
            {
                foreach(var zone in _currentZones)
                {
                    if(_linkedEntity.CurrentZone.Value != zone)
                    {
                        RemoveZone(zone);
                    }
                }
                AddZone(_linkedEntity.CurrentZone.Value!);
                _linkedEntity.CurrentZone.IsDirty = false;
            }

            // Update EntityList
            foreach (var zone in _removeZones)
            {
                if (_currentZones.Remove(zone))
                {
                    foreach (var entity in zone.OldEntities)
                    {
                        DisposeEntity(entity);
                    }
                }
            }

            foreach (var zone in _currentZones)
            {
                foreach (var entity in zone.AddEntities)
                {
                    InitEntity(entity);
                }
                foreach (var entity in zone.RemoveEntities)
                {
                    DisposeEntity(entity);
                }
            }

            foreach (var zone in _addZones)
            {
                if (_currentZones.Add(zone))
                {
                    foreach (var entity in zone.CurrentEntities)
                    {
                        InitEntity(entity);
                    }
                }
            }

            //Write Packet
            foreach (var entity in _disposeEntityList)
            {
                if (_updateEntityList.Remove(entity))
                {
                    MemoryPackSerializer.Serialize<IMPacket, PacketBufferWriter>(_packetBufferWriter, entity.DisposeDataTablePacket());
                }
            }

            foreach (var entity in _updateEntityList)
            {
                MemoryPackSerializer.Serialize<IMPacket, PacketBufferWriter>(_packetBufferWriter, entity.UpdateDataTablePacket());
            }

            foreach (var entity in _initEntityList)
            {
                if (!_updateEntityList.Contains(entity))
                {
                    _updateEntityList.Add(entity);
                    MemoryPackSerializer.Serialize<IMPacket, PacketBufferWriter>(_packetBufferWriter, entity.InitDataTablePacket());
                }
            }

            _disposeEntityList.Clear();
            _initEntityList.Clear();
        }

        /// Zone Method
        public void AddZone(Zone zone)
        {
            if (_currentZones.Contains(zone))
            {
                return;
            }

            _addZones.Add(zone);
        }

        public void RemoveZone(Zone zone)
        {
            if (!_currentZones.Contains(zone))
            {
                return;
            }

            _removeZones.Add(zone);
        }

        /// Entity Method
        private void InitEntity(EntityDataBase entity)
        {
            if (_disposeEntityList.Contains(entity))
            {
                _disposeEntityList.Remove(entity);
                return;
            }

            _initEntityList.Add(entity);
        }

        public void DisposeEntity(EntityDataBase entity)
        {
            if (_initEntityList.Contains(entity))
            {
                _initEntityList.Remove(entity);
                return;
            }

            _disposeEntityList.Add(entity);
        }
    }
}
