
using NetCoreMMOServer.Packet;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace NetCoreMMOServer.Network
{
    public class EntityDataBase
    {
        private static uint s_MaxEntityID = 0;

        private EntityInfo _entityInfo;
        private EntityDataTable _initDataTablePacket;
        private EntityDataTable _updateDataTablePacket;
        private EntityDataTable _disposeDataTablePacket;

        protected List<ISyncData> _syncDatas;

        protected List<ISyncData> _serverSideSyncDatas;
        protected List<ISyncData> _clientSideSyncDatas;

        // Base Param
        public SyncData<bool> IsActive { get; set; } = new(true);
        public SyncData<Vector3> Position { get; set; } = new(new Vector3(0.0f, 0.0f, 0.0f));
        public SyncData<Quaternion> Rotation = new(new Quaternion());
        public SyncData<bool> IsTeleport = new(true);
        public SyncData<Vector3> Velocity { get; set; } = new(new Vector3(0.0f, 0.0f, 0.0f));

        public EntityDataBase(EntityType entityType = EntityType.None)
        {
            _entityInfo.EntityID = ++s_MaxEntityID;

            _initDataTablePacket = new();
            _updateDataTablePacket = new();
            _disposeDataTablePacket = new();
            _disposeDataTablePacket.DataTable.TryAdd(0, IsActive);

            _syncDatas = new(32)
            {
                IsActive,
                Position,
                Velocity,
            };

            _serverSideSyncDatas = new(32)
            {
                IsActive,
                Position,
                Rotation,
                IsTeleport,
            };

            _clientSideSyncDatas = new(32)
            {
                Rotation,
                Velocity,
            };

            Init(_entityInfo);
        }

        /// <summary>
        /// pool에서 가져올 경우, 초기화에서만 사용할 것
        /// </summary>
        /// <param name="entityInfo"></param>
        public void Init(EntityInfo entityInfo)
        {
            _entityInfo = entityInfo;
            _initDataTablePacket.EntityInfo = _entityInfo;
            _updateDataTablePacket.EntityInfo = _entityInfo;
            _disposeDataTablePacket.EntityInfo = _entityInfo;
        }

        public EntityInfo EntityInfo => _entityInfo;
        public EntityType EntityType => _entityInfo.EntityType;
        public uint EntityID => _entityInfo.EntityID;

        public EntityDataTable InitDataTablePacket()
        {
            if (_initDataTablePacket.IsCashed)
            {
                return _initDataTablePacket;
            }

            _initDataTablePacket.IsCashed = true;
            for (byte i = 0; i < _syncDatas.Count; ++i)
            {
                _initDataTablePacket.DataTable.TryAdd(i, _syncDatas[i]);
            }

            return _initDataTablePacket;
        }

        public EntityDataTable UpdateDataTablePacket_Server()
        {
            if (_updateDataTablePacket.IsCashed)
            {
                return _updateDataTablePacket;
            }

            _updateDataTablePacket.IsCashed = true;
            //_position.Value = Transform.Position;
            //_rotation.Value = Transform.Rotation;
            for (byte i = 0; i < _serverSideSyncDatas.Count; ++i)
            {
                if (_serverSideSyncDatas[i].IsDirty)
                {
                    _updateDataTablePacket.DataTable.TryAdd(i, _serverSideSyncDatas[i]);
                    _serverSideSyncDatas[i].IsDirty = false;
                }
            }

            return _updateDataTablePacket;
        }

        public EntityDataTable UpdateDataTablePacket_Client()
        {
            if (_updateDataTablePacket.IsCashed)
            {
                return _updateDataTablePacket;
            }

            _updateDataTablePacket.IsCashed = true;
            for (byte i = 0; i < _clientSideSyncDatas.Count; ++i)
            {
                if (_clientSideSyncDatas[i].IsDirty)
                {
                    _updateDataTablePacket.DataTable.TryAdd(i, _clientSideSyncDatas[i]);
                    _clientSideSyncDatas[i].IsDirty = false;
                }
            }

            return _updateDataTablePacket;
        }

        public EntityDataTable DisposeDataTablePacket()
        {
            return _disposeDataTablePacket;
        }

        public void ClearDataTablePacket()
        {
            _initDataTablePacket.DataTable.Clear();
            _updateDataTablePacket.DataTable.Clear();

            _initDataTablePacket.IsCashed = false;
            _updateDataTablePacket.IsCashed = false;
        }

        public void LoadDataTablePacket_Server(EntityDataTable loadDataTable)
        {
            foreach (var kvp in loadDataTable.DataTable)
            {
                if (kvp.Key < 0 || kvp.Key > _clientSideSyncDatas.Count)
                {
                    Console.WriteLine("Error:: Not Found key");
                    continue;
                }
                _clientSideSyncDatas[kvp.Key].SetValue(kvp.Value);
            }
        }

        public void LoadDataTablePacket_Client(EntityDataTable loadDataTable)
        {
            foreach (var kvp in loadDataTable.DataTable)
            {
                if (kvp.Key < 0 || kvp.Key > _serverSideSyncDatas.Count)
                {
                    Console.WriteLine("Error:: Not Found key");
                    continue;
                }
                _serverSideSyncDatas[kvp.Key].SetValue(kvp.Value);
            }
            //Transform.Position = _position.Value;
            //Transform.Rotation = _rotation.Value;
        }
        public void ClearDataTableDirty()
        {
            foreach (var syncData in _serverSideSyncDatas)
            {
                syncData.IsDirty = false;
            }
            foreach (var syncData in _clientSideSyncDatas)
            {
                syncData.IsDirty = false;
            }
        }
    }
}