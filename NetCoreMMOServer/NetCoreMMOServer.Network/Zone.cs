using MemoryPack;
using NetCoreMMOServer.Packet;
using NetCoreMMOServer.Physics;
using NetCoreMMOServer.Utility;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace NetCoreMMOServer.Network
{
    [StructLayout(LayoutKind.Explicit)]
    public struct ZoneID
    {
        [FieldOffset(0)]
        public int buffer;

        [FieldOffset(0)]
        public byte x;
        [FieldOffset(1)]
        public byte y;
        [FieldOffset(2)]
        public byte z;
        [FieldOffset(3)]
        public byte w;

        public void SetZoneID(Vector3Int zoneCoord)
        {
            x = (byte)zoneCoord.X;
            y = (byte)zoneCoord.Y;
            z = (byte)zoneCoord.Z;
        }

        public void SetZoneID(int x, int y, int z, int w)
        {
            x = (byte)x;
            y = (byte)y;
            z = (byte)z;
            w = (byte)w;
        }
    }

    public record struct ZoneDTO(int Id, string ChunkBinary);

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

        private PacketBufferWriter _chunkBufferWriter;
        private ZoneChunk _zoneChunk;

        public int ZoneID => _zoneID;
        public Vector3Int ZoneCoord => _zoneCoord;
        public Zone[,,] ZoneGridPointer => _zoneGridPointer;
        public Queue<(IMPacket, User)> PacketQueue => _packetQueue;
        public List<EntityDataBase> OldEntities => _oldEntities;
        public List<EntityDataBase> CurrentEntities => _currentEntities;
        public List<EntityDataBase> AddEntities => _addEntities;
        public List<EntityDataBase> RemoveEntities => _removeEntities;
        public ZoneChunk ZoneChunk => _zoneChunk;
        public Vector3 ZonePosition => new Vector3(_zoneCoord.X, _zoneCoord.Y, _zoneCoord.Z) * ZoneOption.ZoneSize - (ZoneOption.ZoneCountXYZ - Vector3.One) * ZoneOption.ZoneSize * 0.5f;

        //public List<EntityDataBase> AddAroundEntities => _aroundEntities;

        public Zone(Vector3Int ZoneCoord, Zone[,,] ZoneGridPointer)
        {
            Init(ZoneCoord);
            _zoneGridPointer = ZoneGridPointer;
            _packetQueue = new Queue<(IMPacket, User)> ();
            _physicsSimulator = new(this);
            _chunkBufferWriter = new(new byte[0xffff]);
            _zoneChunk = new ZoneChunk();
        }

        public void Init(Vector3Int zoneCoord)
        {
            //_zoneID = new ZoneID() { x = (byte)zoneCoord.X, y = (byte)zoneCoord.Y, z = (byte)zoneCoord.Z };
            _zoneID = (zoneCoord.X + 1) * 1000 * 1000 + (zoneCoord.Y + 1) * 1000 + (zoneCoord.Z + 1);
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

        public ZoneDTO GetZoneDTO()
        {
            ZoneDTO zoneDTO = new ZoneDTO();
            zoneDTO.Id = _zoneID;

            for(int i = 0; i < 3; i++)
            {
                for(int j = 0; j < 3; j++)
                {
                    for(int k = 0; k < 3; k++)
                    {
                        _zoneChunk.chunks[i, j, k] = BlockType.None;
                    }
                }
            }

            foreach(var entity in _currentEntities)
            {
                if(entity.EntityType == EntityType.Block)
                {
                    Vector3 blockPosition = entity.Position.Value - ZonePosition + (ZoneOption.ZoneSize - Vector3.One) * 0.5f;
                    Vector3Int blockCoord = new Vector3Int(blockPosition);
                    _zoneChunk.chunks[blockCoord.X, blockCoord.Y, blockCoord.Z] = BlockType.Block;
                }
            }
            MemoryPackSerializer.Serialize<ZoneChunk, PacketBufferWriter>(_chunkBufferWriter, _zoneChunk);
            zoneDTO.ChunkBinary = Encoding.UTF8.GetString(_chunkBufferWriter.GetFilledMemory().Span);

            return zoneDTO;
        }

        public Vector3 GetZonePosition(Vector3Int zoneCoord)
        {
            return new Vector3(zoneCoord.X, zoneCoord.Y, zoneCoord.Z) * ZoneOption.ZoneSize - (ZoneOption.ZoneCountXYZ - Vector3.One) * ZoneOption.ZoneSize * 0.5f;
        }
    }
}
