using NetCoreMMOServer.Packet;
using NetCoreMMOServer.Physics;
using System.Numerics;

namespace NetCoreMMOServer.Framework
{
    public class NetEntity : Entity
    {
        private static uint s_MaxEntityID = 0;

        private EntityInfo _entityInfo;
        private EntityDataTable _initDataTablePacket;
        private EntityDataTable _updateDataTablePacket;
        private EntityDataTable _disposeDataTablePacket;

        protected List<ISyncData> _serverSideSyncDatas;
        protected List<ISyncData> _clientSideSyncDatas;

        // Zone Data
        private SyncData<Zone?> _currentZone;

        // Base Param
        public SyncData<bool> IsActive = new(true);

        // Movement Server Side Param
        private SyncData<Vector3> _position = new(new Vector3());
        private SyncData<Quaternion> _rotation = new(new Quaternion());
        public SyncData<bool> IsTeleport = new(true);


        // Movement Client Side Param
        public SyncData<Vector3> Velocity = new(new Vector3());
        //public SyncData<Vector3> MoveDir = new(new Vector3());

        // Component System

        public NetEntity()
        {
            _entityInfo.EntityType = EntityType.None;
            _entityInfo.EntityID = ++s_MaxEntityID;

            _initDataTablePacket = new();
            _updateDataTablePacket = new();
            _disposeDataTablePacket = new();
            _disposeDataTablePacket.DataTable.TryAdd(0, new SyncData<bool>(false));

            _currentZone = new();

            _serverSideSyncDatas = new(32)
            {
                IsActive,
                _position,
                _rotation,
                IsTeleport,
            };

            _clientSideSyncDatas = new(32)
            {
                _rotation,
                Velocity,
            };

            Init(_entityInfo);
        }

        /// <summary>
        /// pool에서 가져올 경우, 초기화에서만 사용할 것
        /// Override를 통해서 하위에서 초기화하는 내용도 추가해야함.
        /// </summary>
        /// <param name="entityInfo"></param>
        public void Init(EntityInfo entityInfo)
        {
            _entityInfo = entityInfo;
            _initDataTablePacket.EntityInfo = _entityInfo;
            _updateDataTablePacket.EntityInfo = _entityInfo;
            _disposeDataTablePacket.EntityInfo = _entityInfo;
            _currentZone.Value = null;
        }
        public void Init(uint entityID, EntityType entityType)
        {
            Init(new EntityInfo(entityID, entityType));
        }

        public EntityInfo EntityInfo => _entityInfo;
        public EntityType EntityType => _entityInfo.EntityType;
        public uint EntityID => _entityInfo.EntityID;
        public SyncData<Zone?> CurrentZone => _currentZone;

        public override void Update(float dt)
        {

        }

        public void Teleport(Vector3 position)
        {
            Transform.Position = position;
            _position.ForceSetValue(position);
            IsTeleport.ForceSetValue(true);
        }

        public EntityDataTable InitDataTablePacket()
        {
            if (_initDataTablePacket.IsCashed)
            {
                return _initDataTablePacket;
            }

            _initDataTablePacket.IsCashed = true;
            for (byte i = 0; i < _serverSideSyncDatas.Count; ++i)
            {
                _initDataTablePacket.DataTable.TryAdd(i, _serverSideSyncDatas[i]);
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
            _position.Value = Transform.Position;
            _rotation.Value = Transform.Rotation;
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
            Transform.Position = _position.Value;
            Transform.Rotation = _rotation.Value;
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

        public void MoveZone(Zone moveZone)
        {
            if (_currentZone.Value?.ZoneCoord == moveZone.ZoneCoord)
            {
                return;
            }

            if (_currentZone.Value != null)
            {
                _currentZone.Value.RemoveEntity(this);
            }
            _currentZone.Value = moveZone;
            _currentZone.Value.AddEntity(this);
        }
    }
}
