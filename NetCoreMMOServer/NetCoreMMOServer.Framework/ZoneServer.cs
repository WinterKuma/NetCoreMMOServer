using NetCoreMMOServer.Network;
using NetCoreMMOServer.Packet;
using NetCoreMMOServer.Utility;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Sockets;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace NetCoreMMOServer.Framework
{
    public abstract class ZoneServer : Server
    {
        // Server Option
        private bool _isRun = true;
        private uint _frameRate = 33;

        // Time
        private long _deltaMilliseconds = 0;
        private float _dt = 0.0f;

        // User Control
        private readonly SwapChain<List<User>> _connectUserSwapChain;
        private readonly SwapChain<List<User>> _disconnectUserSwapChain;
        private readonly List<User> _userList;

        private readonly ConcurrentPool<User> _userPool;
        private readonly Dictionary<uint, User> _userIDDictionary;

        // Packet Control
        private readonly SwapChain<Queue<(IMPacket, User)>> _packetQueueSwapChain;

        // Zone Control
        private readonly List<Zone> _zoneList;
        private readonly Zone[,,] _zones;
        //private EntityDataBase?[,,] _groundEntities;

        // Entity Control
        private readonly ConcurrentDictionary<Type, ConcurrentPool<NetEntity>> _entityPoolTable;
        private readonly ConcurrentDictionary<NetEntity, ConcurrentPool<NetEntity>> _entityObjectPoolTable;
        private readonly ConcurrentDictionary<EntityInfo, NetEntity> _entityTable;

        // Physics Control
        private float _deltaAcc = 0.0f;

        public ZoneServer(int port) : base(port)
        {
            _connectUserSwapChain = new();
            _disconnectUserSwapChain = new();
            _userList = new();

            _userPool = new();
            _userIDDictionary = new();

            _packetQueueSwapChain = new();

            _zoneList = new(ZoneOption.ZoneCountX * ZoneOption.ZoneCountY * ZoneOption.ZoneCountZ);
            _zones = new Zone[ZoneOption.ZoneCountX, ZoneOption.ZoneCountY, ZoneOption.ZoneCountZ];
            for (int x = 0; x < ZoneOption.ZoneCountX; ++x)
            {
                for (int y = 0; y < ZoneOption.ZoneCountY; ++y)
                {
                    for (int z = 0; z < ZoneOption.ZoneCountZ; ++z)
                    {
                        _zones[x, y, z] = new Zone(new Vector3Int(x, y, z), _zones);
                        _zoneList.Add(_zones[x, y, z]);
                    }
                }
            }

            _entityPoolTable = new();
            _entityObjectPoolTable = new();
            _entityTable = new();
        }

        public long DeltaMilliseconds => _deltaMilliseconds;
        public float Dt => _dt;
        public uint FrameRate
        {
            get { return _frameRate; }
            set { _frameRate = value; }
        }

        public List<User> UserList => _userList;
        public Dictionary<uint, User> UserDictionary => _userIDDictionary;
        public List<Zone> ZoneList => _zoneList;
        public ConcurrentDictionary<EntityInfo, NetEntity> EntityTable => _entityTable;

        protected override async Task Accepted(Socket socket)
        {
            var client = _userPool.Get();
            client.Init(socket);
            client.Received ??= ReceivePacket;
            RegisterConnectUser(client);

            try
            {
                await client.ReceiveAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error:: Failed!! [Client ReceiveAsync] : {ex}");
            }
            finally
            {
                await client.CompleteAsync();
                client.Disconnect(false);
            }

            RegisterDisconnectUser(client);
        }

        protected override Task<bool> Accepting(Socket socket)
        {
            return Task.FromResult(true);
        }

        protected abstract void Initialize();
        protected abstract void Release();
        protected abstract void Update(float dt);
        protected abstract void ProcessPacket(in IMPacket packet, in User user);
        protected abstract void ConnectedUser(User user);
        protected abstract void DisconnectedUser(User user);

        public void Run()
        {
            Start();
            Console.WriteLine("[Framework] : Server Start.");
            _isRun = true;

            Initialize();
            Console.WriteLine("[Framework] : Server Initialize.");

            try
            {
                Stopwatch watch = new();
                watch.Start();
                while (_isRun)
                {
                    _deltaMilliseconds = watch.ElapsedMilliseconds;
                    _dt = _deltaMilliseconds * 0.001f;
                    watch.Restart();

                    foreach (var user in _userList)
                    {
                        user.ClearWriter();
                    }

                    ProcessConnectUser();

                    var packetQueue = _packetQueueSwapChain.Swap();
                    while (packetQueue.Count > 0)
                    {
                        if (packetQueue.TryDequeue(out var packet))
                        {
                            ProcessPacket(packet.Item1, packet.Item2);
                            PacketPool.ReturnPacket(packet.Item1);
                        }
                    }

                    Update(_dt);

                    _deltaAcc += _dt;
                    if (_deltaAcc > 1)
                    {
                        _deltaAcc = 1;
                    }

                    Parallel.ForEach(_zoneList, zone =>
                    {
                        zone.Update(_dt);
                        zone.FixedUpdate(_dt);
                    });

                    //foreach (var zone in _zoneList)
                    //{
                    //    zone.Update(_dt);
                    //    zone.FixedUpdate(_dt);
                    //}

                    while (_deltaAcc >= PhysicsOption.IntervalDelta)
                    {
                        Parallel.ForEach(_zoneList, zone =>
                        {
                            zone.Step(PhysicsOption.IntervalDelta);
                        });
                        //foreach (var zone in _zoneList)
                        //{
                        //    zone.Step(PhysicsOption.IntervalDelta);
                        //}
                        _deltaAcc -= PhysicsOption.IntervalDelta;
                    }

                    ProcessDisconnectUser();

                    foreach (var entity in _entityTable.Values)
                    {
                        SetZone(entity);
                    }

                    foreach (var user in _userList)
                    {
                        user.WritePacket();
                        user.SendAsync(user.PacketBufferWriter.GetFilledMemory()).ConfigureAwait(false);
                        user.LinkedEntity?.ClearDataTablePacket();
                    }

                    foreach (var zone in _zones)
                    {
                        zone.ResetAndBackupEntityList();
                    }

                    //Console.WriteLine(watch.ElapsedMilliseconds);
                    if (watch.ElapsedMilliseconds < _frameRate)
                    {
                        Thread.Sleep(Math.Max(0, (int)(_frameRate - watch.ElapsedMilliseconds)));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Framework] : Server Loop Error Exceptions.");
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                Release();
                Console.WriteLine($"[Framework] : Server Release");
            }
        }

        public void Stop()
        {
            _isRun = false;
        }


        // User Connect / Disconnect
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RegisterConnectUser(User user)
        {
            lock (_connectUserSwapChain.CurrentBuffer)
            {
                _connectUserSwapChain.CurrentBuffer.Add(user);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RegisterDisconnectUser(User user)
        {
            lock (_disconnectUserSwapChain.CurrentBuffer)
            {
                _disconnectUserSwapChain.CurrentBuffer.Add(user);
            }
        }

        private void ProcessConnectUser()
        {
            var connectUserList = _connectUserSwapChain.Swap();
            if (connectUserList.Count == 0)
            {
                return;
            }

            foreach (var user in connectUserList)
            {
                if (!_userIDDictionary.TryAdd(user.ID, user))
                {
                    Console.WriteLine($"Error:: Failed!! [ProcessConnectUser] => TryAdd : false");
                    user.Disconnect(false);
                    continue;
                }

                _userList.Add(user);

                ConnectedUser(user);
            }

            connectUserList.Clear();
        }

        private void ProcessDisconnectUser()
        {
            var disconnectUserList = _disconnectUserSwapChain.Swap();
            if (disconnectUserList.Count == 0)
            {
                return;
            }

            foreach (var user in disconnectUserList)
            {
                if (!_userIDDictionary.Remove(user.ID))
                {
                    Console.WriteLine($"Error:: Failed!! [ProcessDisconnectUser] => Remove : false");
                }

                DisconnectedUser(user);

                _userList.Remove(user);
                _userPool.Return(user);
            }

            disconnectUserList.Clear();
        }

        // Packet
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReceivePacket(IMPacket packet, PipeSocket user)
        {
            lock (_packetQueueSwapChain.CurrentBuffer)
            {
                _packetQueueSwapChain.CurrentBuffer.Enqueue((packet, (User)user));
            }
        }


        // Zone
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Zone GetZone(Vector3Int zoneCoord)
        {
            return _zones[zoneCoord.X, zoneCoord.Y, zoneCoord.Z];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Zone GetZone(int x, int y, int z)
        {
            return _zones[x, y, z];
        }

        protected virtual void SetZone(NetEntity entity)
        {
            Vector3 pos = entity.Position.Value;
            if (pos.X > ZoneOption.TotalZoneHalfWidth ||
                pos.Y > ZoneOption.TotalZoneHalfHeight ||
                pos.Z > ZoneOption.TotalZoneHalfDepth ||
                pos.X < -ZoneOption.TotalZoneHalfWidth ||
                pos.Y < -ZoneOption.TotalZoneHalfHeight ||
                pos.Z < -ZoneOption.TotalZoneHalfDepth)
            {
                entity.Transform.Position = Vector3.Zero;
                entity.Position.Value = Vector3.Zero;
                entity.Position.IsDirty = true;
                entity.Velocity.Value = Vector3.UnitY;
                entity.Velocity.IsDirty = true;
                //entity.MoveZone(_zones[1, 1]);
                //return;
                pos = entity.Position.Value;
            }

            int x = Math.Clamp((int)((pos.X + ZoneOption.TotalZoneHalfWidth) * ZoneOption.InverseZoneWidth), 0, ZoneOption.ZoneCountX - 1);
            int y = Math.Clamp((int)((pos.Y + ZoneOption.TotalZoneHalfHeight) * ZoneOption.InverseZoneHeight), 0, ZoneOption.ZoneCountY - 1);
            int z = Math.Clamp((int)((pos.Z + ZoneOption.TotalZoneHalfDepth) * ZoneOption.InverseZoneDepth), 0, ZoneOption.ZoneCountZ - 1);
            entity.MoveZone(_zones[x, y, z]);
        }

        // Entity
        protected bool CreateEntity<T>(out NetEntity entity, Vector3 position) where T : NetEntity, new()
        {
            if (!_entityPoolTable.TryGetValue(typeof(T), out var entityPool))
            {
                entityPool = new ConcurrentPool<NetEntity>();
            }

            entity = entityPool.Get<T>();
            entity.Transform.Position = position;

            if (!_entityObjectPoolTable.TryAdd(entity, entityPool))
            {
                Console.WriteLine($"Error:: Failed!! => _entityObjectPoolTable.TryAdd({entity.EntityInfo}, {entity})");
                entityPool.Return(entity);
                return false;
            }

            if (!_entityTable.TryAdd(entity.EntityInfo, entity))
            {
                Console.WriteLine($"Error:: Failed!! => _entityTable.TryAdd({entity.EntityInfo}, {entity})");
                entityPool.Return(entity);
                return false;
            }

            return true;
        }

        protected bool ReleaseEntity(NetEntity entity)
        {
            if (!_entityTable.TryRemove(entity.EntityInfo, out _))
            {
                Console.WriteLine($"Error:: Failed!! => _entityTable.TryAdd({entity.EntityInfo}, {entity})");
                return false;
            }

            entity.CurrentZone.Value?.RemoveEntity(entity);

            if (!_entityObjectPoolTable.TryRemove(entity, out var entityPool))
            {
                if (_entityObjectPoolTable.ContainsKey(entity))
                {
                    Console.WriteLine($"Error:: Failed!! => _entityObjectPoolTable.TryRemove({entity})");
                }
                else
                {
                    Console.WriteLine($"Error:: Failed!! => {entity} is Not Pool Object!!)");
                }
                return false;
            }
            entityPool.Return(entity);

            return true;
        }
    }
}
