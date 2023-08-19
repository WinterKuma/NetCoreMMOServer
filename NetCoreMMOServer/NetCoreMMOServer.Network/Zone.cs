using NetCoreMMOServer.Packet;
using NetCoreMMOServer.Physics;
using NetCoreMMOServer.Utility;

namespace NetCoreMMOServer.Network
{
    public class Zone
    {
        private int _zoneID;
        private Vector3Int _zoneCoord;
        private Zone[,,] _zoneGridPointer;
        private Queue<(IMPacket, User)> _packetQueue;

        private List<EntityDataBase> _oldEntities = new();
        private List<EntityDataBase> _currentEntities = new();
        private List<EntityDataBase> _addEntities = new();
        private List<EntityDataBase> _removeEntities = new();

        private ZoneSimulator _physicsSimulator;
        private List<EntityDataBase> _aroundEntities = new();
        private Vector3Int _minZoneCoord;
        private Vector3Int _maxZoneCoord;

        public int ZoneID => _zoneID;
        public Vector3Int ZoneCoord => _zoneCoord;
        public Zone[,,] ZoneGridPointer => _zoneGridPointer;
        public Queue<(IMPacket, User)> PacketQueue => _packetQueue;
        public List<EntityDataBase> OldEntities => _oldEntities;
        public List<EntityDataBase> CurrentEntities => _currentEntities;
        public List<EntityDataBase> AddEntities => _addEntities;
        public List<EntityDataBase> RemoveEntities => _removeEntities;

        //public List<EntityDataBase> AddAroundEntities => _aroundEntities;

        public Zone(Vector3Int ZoneCoord, Zone[,,] ZoneGridPointer)
        {
            Init(ZoneCoord);
            _zoneGridPointer = ZoneGridPointer;
            _packetQueue = new Queue<(IMPacket, User)> ();
            _physicsSimulator = new(this);
        }

        public void Init(Vector3Int zoneCoord)
        {
            _zoneID = (zoneCoord.X * 10000 + zoneCoord.Y * 100 + zoneCoord.Z);
            _zoneCoord = zoneCoord;
            _currentEntities.Clear();
            _minZoneCoord = new Vector3Int(Math.Max(0, _zoneCoord.X - 1), Math.Max(0, _zoneCoord.Y - 1), Math.Max(0, _zoneCoord.Z - 1));
            _maxZoneCoord = new Vector3Int(Math.Min(ZoneOption.ZoneCountX - 1, _zoneCoord.X + 1), Math.Min(ZoneOption.ZoneCountY - 1, _zoneCoord.Y + 1), Math.Min(ZoneOption.ZoneCountZ - 1, _zoneCoord.Z + 1));
        }

        public void FixedUpdate(float dt)
        {
            _aroundEntities.Clear();
            _physicsSimulator.ResetEntity();

            for (int x = _minZoneCoord.X; x <= _maxZoneCoord.X; x++)
            {
                for (int y = _minZoneCoord.Y; y <= _maxZoneCoord.Y; y++)
                {
                    for (int z = _minZoneCoord.Z; z <= _maxZoneCoord.Z; z++)
                    {
                        foreach(var entity in _zoneGridPointer[x, y, z]._currentEntities)
                        {
                            _physicsSimulator.AddEntity(entity);
                            //_addEntities.Add(entity);
                        }
                    }
                }
            }

            //_physicsSimulator.ResetEntity();

            //foreach (var entity in _aroundEntities)
            //{
            //    _physicsSimulator.AddEntity(entity);
            //}

            _physicsSimulator.Update(dt);
        }

        public void Step(float time)
        {
            _physicsSimulator.Step(time);
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
