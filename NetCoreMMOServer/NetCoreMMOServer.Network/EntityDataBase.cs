using NetCoreMMOServer.Packet;
using NetCoreMMOServer.Physics;
using System.Numerics;

namespace NetCoreMMOServer.Network
{
    // preview
    public partial class PlayerEntity : EntityDataBase
    {
        public SyncData<int> power = new(1);
        public SyncData<int> hp = new(10);
    }

    public partial class PlayerEntity : EntityDataBase
    {
        public PlayerEntity()
        {
            _syncDatas.Add(power);
            _syncDatas.Add(hp);
        }
    }


    public class EntityDataBase
    {
        private static uint s_MaxEntityID = 0;

        private EntityInfo _entityInfo;
        private EntityDataTable _initDataTablePacket;
        private EntityDataTable _updateDataTablePacket;
        private EntityDataTable _disposeDataTablePacket;
        protected List<ISyncData> _syncDatas;

        // Zone Data
        private SyncData<Zone?> _currentZone;

        // Base Param
        public SyncData<bool> IsActive = new(true);
        public SyncData<Vector3> Position = new(new Vector3(0.0f, 0.0f, 0.0f));
        public SyncData<Vector3> Velocity = new(new Vector3(0.0f, 0.0f, 0.0f));

        // Component System
        public List<Component.Component> components;

        public EntityDataBase()
        {
            _entityInfo.EntityID = ++s_MaxEntityID;

            _initDataTablePacket = new();
            _updateDataTablePacket = new();
            _disposeDataTablePacket = new();
            _disposeDataTablePacket.DataTable.TryAdd(0, new SyncData<bool>(false));

            _currentZone = new();

            _syncDatas = new(32)
            {
                IsActive,
                Position,
                Velocity,
            };

            components = new(12);

            RigidBody rigidBody = new();
            SphereCollider collider = new();
            collider.Offset = Vector3.Zero;
            collider.Radius = 0.5f;
            collider.AttachedRigidbody = rigidBody;

            rigidBody.SetEntityDataBase(this);
            collider.SetEntityDataBase(this);

            components.Add(rigidBody);
            components.Add(collider);

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
            _currentZone.Value = null;

            //components.Clear();
        }

        public EntityInfo EntityInfo => _entityInfo;
        public EntityType EntityType => _entityInfo.EntityType;
        public uint EntityID => _entityInfo.EntityID;
        public SyncData<Zone?> CurrentZone => _currentZone;

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

        public void MoveZone(Zone moveZone)
        {
            if(_currentZone.Value?.ZoneCoord == moveZone.ZoneCoord)
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
