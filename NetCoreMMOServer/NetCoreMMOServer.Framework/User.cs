using MemoryPack;
using NetCoreMMOServer.Network;
using NetCoreMMOServer.Packet;
using System.IO.Pipelines;
using System.Net.Sockets;

namespace NetCoreMMOServer.Framework
{
    public class User : PipeSocket
    {
        private static uint s_MaxID = 0;
        private readonly uint _id;

        //private PipeSocket? _pipeSocket;
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

            _linkedEntity = null;

            _packetBufferWriter = new(new byte[0xffff]);
            _currentZones = new();
            _addZones = new();
            _removeZones = new();

            _updateEntityList = new();
            _initEntityList = new();
            _disposeEntityList = new();
        }

        //public User(int id, Socket? socket = null, IDuplexPipe? pipe = null) : this()
        //{
        //    _id = id;

        //    if (socket != null)
        //    {
        //        _socket = socket;
        //        if (pipe == null)
        //        {
        //            _pipe = new DuplexPipe(new NetworkStream(socket));
        //        }
        //    }

        //    if (pipe != null)
        //    {
        //        _pipe = pipe;
        //    }
        //}

        public void Init(Socket socket)
        {
            //_pipeSocket = new PipeSocket(socket);
            SetSocket(socket);

            _currentZones.Clear();
            _addZones.Clear();
            _removeZones.Clear();
            _updateEntityList.Clear();
            _initEntityList.Clear();
            _disposeEntityList.Clear();
        }

        public uint ID => _id;
        public EntityDataBase? LinkedEntity => _linkedEntity;
        public ref PacketBufferWriter PacketBufferWriter => ref _packetBufferWriter;

        public async Task CompleteAsync()
        {
            await Reader.CompleteAsync();
            await Writer.CompleteAsync();
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

            if (_linkedEntity?.CurrentZone.IsDirty ?? false)
            {
                if (_linkedEntity?.CurrentZone.Value == null)
                {
                    return;
                }
                Zone nextZone = _linkedEntity.CurrentZone.Value;

                // Remove Zone
                foreach (var zone in _currentZones)
                {
                    int x = Math.Abs(zone.ZoneCoord.X - nextZone.ZoneCoord.X);
                    int y = Math.Abs(zone.ZoneCoord.Y - nextZone.ZoneCoord.Y);
                    int z = Math.Abs(zone.ZoneCoord.Z - nextZone.ZoneCoord.Z);

                    if (x >= ZoneOption.RemoveZoneRangeX ||
                        y >= ZoneOption.RemoveZoneRangeY ||
                        z >= ZoneOption.RemoveZoneRangeZ)
                    {
                        RemoveZone(zone);
                    }
                }

                for (int x = Math.Max(0, nextZone.ZoneCoord.X - ZoneOption.AddZoneRangeX); x <= Math.Min(ZoneOption.ZoneCountX - 1, nextZone.ZoneCoord.X + ZoneOption.AddZoneRangeX); x++)
                {
                    for (int y = Math.Max(0, nextZone.ZoneCoord.Y - ZoneOption.AddZoneRangeY); y <= Math.Min(ZoneOption.ZoneCountY - 1, nextZone.ZoneCoord.Y + ZoneOption.AddZoneRangeY); y++)
                    {
                        for (int z = Math.Max(0, nextZone.ZoneCoord.Z - ZoneOption.AddZoneRangeZ); z <= Math.Min(ZoneOption.ZoneCountZ - 1, nextZone.ZoneCoord.Z + ZoneOption.AddZoneRangeZ); z++)
                        {
                            AddZone(nextZone.ZoneGridPointer[x, y, z]);
                        }
                    }
                }
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
