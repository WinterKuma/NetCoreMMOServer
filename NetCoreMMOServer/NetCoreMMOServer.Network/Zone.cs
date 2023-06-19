using NetCoreMMOServer.Packet;
using NetCoreMMOServer.Utility;

namespace NetCoreMMOServer.Network
{
    public class Zone
    {
        private int _zoneID;
        private Vector3Int _zoneCoord;

        private List<EntityDataBase> _oldEntities = new();
        private List<EntityDataBase> _currentEntities = new();
        private List<EntityDataBase> _addEntities = new();
        private List<EntityDataBase> _removeEntities = new();

        public int ZoneID => _zoneID;
        public Vector3Int ZoneCoord => _zoneCoord;
        public List<EntityDataBase> OldEntities => _oldEntities;
        public List<EntityDataBase> CurrentEntities => _currentEntities;
        public List<EntityDataBase> AddEntities => _addEntities;
        public List<EntityDataBase> RemoveEntities => _removeEntities;

        public void Init(Vector3Int zoneCoord)
        {
            _zoneID = (zoneCoord.X * 10000 + zoneCoord.Y * 100 + zoneCoord.Z);
            _zoneCoord = zoneCoord;
            _currentEntities.Clear();
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
