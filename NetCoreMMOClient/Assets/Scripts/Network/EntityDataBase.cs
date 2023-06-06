using NetCoreMMOServer.Packet;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NetCoreMMOServer.Network
{
    public class EntityDataBase
    {
        private static uint s_MaxEntityID = 0;

        private EntityInfo _entityInfo;
        private EntityDataTable _initDataTablePacket;
        private EntityDataTable _updateDataTablePacket;
        private EntityDataTable _disposeDataTablePacket;
        private List<ISyncData> _syncDatas;

        // Base Param
        public SyncData<bool> IsActive { get; set; } = new(true);
        public SyncData<Vector3> Position { get; set; } = new(new Vector3(0.0f, 0.0f, 0.0f));

        public EntityDataBase()
        {
            _entityInfo.EntityID = ++s_MaxEntityID;

            _initDataTablePacket = new();
            _updateDataTablePacket = new();
            _disposeDataTablePacket = new();
            _disposeDataTablePacket.DataTable.TryAdd(0, IsActive);

            _syncDatas = new(32)
            {
                IsActive,
                Position
            };

            Init(_entityInfo);
        }

        /// <summary>
        /// pool���� ������ ���, �ʱ�ȭ������ ����� ��
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

        public EntityDataTable UpdateDataTablePacket()
        {
            if (_updateDataTablePacket.IsCashed)
            {
                return _updateDataTablePacket;
            }

            _updateDataTablePacket.IsCashed = true;
            for (byte i = 0; i < _syncDatas.Count; ++i)
            {
                if (_syncDatas[i].IsDirty)
                {
                    _updateDataTablePacket.DataTable.TryAdd(i, _syncDatas[i]);
                    _syncDatas[i].IsDirty = false;
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

        public void LoadDataTablePacket(EntityDataTable loadDataTable)
        {
            foreach (var kvp in loadDataTable.DataTable)
            {
                if (kvp.Key < 0 || kvp.Key > _syncDatas.Count)
                {
                    Console.WriteLine("Error:: Not Found key");
                    continue;
                }
                _syncDatas[kvp.Key].SetValue(kvp.Value);
            }
        }
    }
}