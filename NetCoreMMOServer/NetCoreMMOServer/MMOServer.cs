using MemoryPack;
using NetCoreMMOServer.Contents.Entity;
using NetCoreMMOServer.Framework;
using NetCoreMMOServer.Network;
using NetCoreMMOServer.Network.Components.Contents;
using NetCoreMMOServer.Packet;
using NetCoreMMOServer.Physics;
using NetCoreMMOServer.Utility;
using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Numerics;
using System.Text;

namespace NetCoreMMOServer
{
    internal class MMOServer
    {
        private Server? _server;
        private readonly int _port;

        private SwapChain<List<User>> _connectUserSwapChain;
        private SwapChain<List<User>> _disconnectUserSwapChain;
        private List<User> _userList;

        private ConcurrentPool<User> _userPool;
        private Dictionary<int, User> _userIDDictionary;

        private SwapChain<Queue<(IMPacket, User)>> _packetQueueSwapChain;
        //private Queue<IMPacket> _broadcastPacketQueue;

        private ConcurrentPool<PacketBufferWriter> _rpcPacketBufferWriterPool;

        private List<Zone> _zoneList;
        private Zone[,,] _zones;
        private EntityDataBase?[,,] _groundEntities;
        private Dictionary<EntityInfo, EntityDataBase> _entityTable;
        //private ConcurrentPool<EntityDataBase> _entityDataBasePool;

        //Packet Instance
        private SetLinkedEntityPacket _setLinkedEntityPacket;

        private Simulator _physiscSimulator;
        private HttpClient _httpClient;

        public MMOServer(int port)
        {
            _port = port;

            _userList = new();
            _connectUserSwapChain = new();
            _disconnectUserSwapChain = new();

            _userPool = new();
            _userIDDictionary = new();

            _packetQueueSwapChain = new();
            //_broadcastPacketQueue = new();

            _zoneList = new List<Zone>(ZoneOption.ZoneCountX * ZoneOption.ZoneCountY * ZoneOption.ZoneCountZ);
            _zones = new Zone[ZoneOption.ZoneCountX, ZoneOption.ZoneCountY, ZoneOption.ZoneCountZ];
            for (int x = 0; x < ZoneOption.ZoneCountX; ++x)
            {
                for (int y = 0; y < ZoneOption.ZoneCountY; ++y)
                {
                    for (int z = 0; z < ZoneOption.ZoneCountZ; ++z)
                    {
                        _zones[x, y, z] = new Zone(new Vector3Int(x, y, z), _zones);
                        _zoneList.Add(_zones[x, y, z]);
                        //_zones[x, y, z].Init(new Vector3Int(x, y, z));
                    }
                }
            }

            _entityTable = new();
            _groundEntities = new EntityDataBase[(int)MathF.Ceiling(ZoneOption.TotalZoneWidth), (int)MathF.Ceiling(ZoneOption.TotalZoneHeight), (int)MathF.Ceiling(ZoneOption.TotalZoneDepth)];

            //for (int x = (int)MathF.Ceiling(-ZoneOption.TotalZoneHalfWidth); x < ZoneOption.TotalZoneHalfWidth; x++)
            //{
            //    for (int z = (int)MathF.Ceiling(-ZoneOption.TotalZoneHalfDepth); z < ZoneOption.TotalZoneHalfDepth; z++)
            //    {
            //        if (!CreateEntity(EntityType.Block, out EntityDataBase entity, new Vector3(x, -2f, z)))
            //        {
            //            Console.WriteLine($"Error:: Don't Create Entity [{EntityType.Block}]");
            //            continue;
            //        }
            //        //entity.Position.Value = new Vector3(x, -2f, z);
            //        //Vector3Int gPos = new Vector3Int(entity.Position.Value + ZoneOption.TotalZoneHalfSize);
            //        //_groundEntities[gPos.X, gPos.Y, gPos.Z] = entity;
            //    }
            //}
            _httpClient = new HttpClient() { BaseAddress = new Uri("https://localhost:7251") };

            _setLinkedEntityPacket = new();

            _physiscSimulator = new();
        }

        public async Task StartAsync(int backlog = (int)SocketOptionName.MaxConnections)
        {
            _server = new(_port);
            _server.Accepted += AcceptAsync;
            _server.Start(backlog);

            CommandAsync();

            foreach (var zone in _zones)
            {
                await LoadZoneDB(zone.ZoneCoord);
            }

            try
            {
                Update();
            }
            catch (Exception ex)
            {
                
            }
            finally
            {
                foreach (var zone in _zones)
                {
                    await SaveZoneDB(zone.ZoneCoord);
                }
            }
        }

        public async Task CommandAsync()
        {
            await Task.Run(async () =>
            {
                while (true)
                {
                    if (Console.KeyAvailable)
                    {
                        ConsoleKeyInfo keyInfo = Console.ReadKey(intercept: true);
                        Console.WriteLine($"눌린 키: {keyInfo.Key}");
                        if (keyInfo.Key == ConsoleKey.S)
                        {
                            foreach (var zone in _zones)
                            {
                                await SaveZoneDB(zone.ZoneCoord);
                            }
                        }
                    }
                }
            });
        }

        private void Update()
        {
            Stopwatch st = new();
            st.Start();
            while (true)
            {
                long deltaMilliseconds = st.ElapsedMilliseconds;
                float dt = deltaMilliseconds / 1000.0f;
                st.Restart();

                // User Writer Buffer Clear
                foreach (var user in _userList)
                {
                    user.ClearWriter();
                }

                // Update Connect User;
                ProcessConnectUser();

                // Update Packet
                var packetQueue = _packetQueueSwapChain.Swap();
                while (packetQueue.Count > 0)
                {
                    if (packetQueue.TryDequeue(out var packet))
                    {
                        //packet.Item2?.LinkedEntity?.CurrentZone.Value?.PacketQueue.Enqueue(packet);
                        ProcessPacket(packet.Item1, packet.Item2);
                        PacketPool.ReturnPacket(packet.Item1);
                    }
                }

                // Update Zone Threading
                //Parallel.ForEach(_zoneList,
                //    zone =>
                //    {
                //        while (zone.PacketQueue.Count > 0)
                //        {
                //            if (zone.PacketQueue.TryDequeue(out var packet))
                //            {
                //                ProcessPacket(packet.Item1);
                //                PacketPool.ReturnPacket(packet.Item1);
                //            }
                //        }
                //    });

                //_zoneList.AsParallel().WithDegreeOfParallelism(2).ForAll(
                //    zone =>
                //{
                //    while (zone.PacketQueue.Count > 0)
                //    {
                //        if (zone.PacketQueue.TryDequeue(out var packet))
                //        {
                //            ProcessPacket(packet.Item1);
                //            PacketPool.ReturnPacket(packet.Item1);
                //        }
                //    }
                //});

                //_physiscSimulator.ResetEntity();
                //
                //foreach (var entity in _entityTable.Values)
                //{
                //    _physiscSimulator.AddEntity(entity);
                //}
                //
                //_physiscSimulator.Update(dt);

                Parallel.ForEach(_zoneList,
                    zone =>
                    {
                        zone.FixedUpdate(dt);
                    });

                for (int i = 0; i < PhysicsOption.StepCount; i++)
                {
                    Parallel.ForEach(_zoneList,
                        zone =>
                        {
                            zone.Step(dt * PhysicsOption.InverseStepCount);
                        });
                }

                // Update Disconnect User
                ProcessDisconnectUser();

                // Update Zone (with. Entity)
                foreach (var entity in _entityTable.Values)
                {
                    SetZone(entity);
                }


                // Update User & Send Packet
                foreach (var user in _userList)
                {
                    user.WritePacket();
                    SendAsync(user, user.PacketBufferWriter.GetFilledMemory());
                }

                foreach (var user in _userList)
                {
                    user.LinkedEntity?.ClearDataTablePacket();
                }

                // Reset And Backup Zone EntityList
                foreach (var zone in _zones)
                {
                    zone.ResetAndBackupEntityList();
                }

                // Sleep MainLoop Thread
                if (st.ElapsedMilliseconds < 100)
                {
                    //Thread.Sleep(1);
                    Console.WriteLine($"Log:: ElapsedMilliseconds : {st.ElapsedMilliseconds}");
                    Thread.Sleep(Math.Max(0, (int)(100 - st.ElapsedMilliseconds)));
                }
                else
                {
                    Console.WriteLine($"Log:: Hight Tick ElapsedMilliseconds : {st.ElapsedMilliseconds}");
                }
            }
        }

        private async ValueTask AcceptAsync(Socket socket)
        {
            Console.WriteLine($"Log::Socket[{socket.RemoteEndPoint}]: connected");

            var client = _userPool.Get();
            client.Init(socket);
            RegisterConnectUser(client);

            try
            {
                await Task.WhenAll(ReceiveAsync(client));
                await client.CompleteAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            RegisterDisconnectUser(client);

            Console.WriteLine($"Log::Socket[{socket.RemoteEndPoint}]: disconnected");
            socket.Disconnect(false);
            socket.Close();
        }

        private async ValueTask SendAsync(User client, ReadOnlyMemory<byte> buffer)
        {
            _ = await client.Writer.WriteAsync(buffer).ConfigureAwait(false);
        }

        private async ValueTask SendRPCAsync(User client, PacketBufferWriter packetBufferWriter)
        {
            _ = await client.Writer.WriteAsync(packetBufferWriter.GetFilledMemory()).ConfigureAwait(false);
            packetBufferWriter.Clear();
            _rpcPacketBufferWriterPool.Return(packetBufferWriter);
        }

        private void SendBroadcast(ReadOnlyMemory<byte> buffer)
        {
            foreach (var user in _userList)
            {
                _ = SendAsync(user, buffer);
            }
        }

        private void SendAnother(User client, ReadOnlyMemory<byte> buffer)
        {
            foreach (var user in _userList)
            {
                if (user == client) continue;
                _ = SendAsync(user, buffer);
            }
        }

        private async Task ReceiveAsync(User user)
        {
            try
            {
                while (true)
                {
                    ReadResult result = await user.Reader.ReadAsync().ConfigureAwait(false);
                    ReadOnlySequence<byte> buffer = result.Buffer;

                    while (BufferResolver.TryReadPacket(ref buffer, out var packet))
                    {
                        if (packet == null) continue;

                        lock (_packetQueueSwapChain.CurrentBuffer)
                        {
                            _packetQueueSwapChain.CurrentBuffer.Enqueue((packet, user));
                        }
                    }

                    // Tell the PipeReader how much of the buffer has been consumed.
                    user.Reader.AdvanceTo(buffer.Start, buffer.End);

                    // Stop reading if there's no more data coming.
                    if (result.IsCompleted)
                    {
                        break;
                    }
                }
            }
            catch
            {
                Console.WriteLine($"Disconnect User[{user.Socket}]");
            }
        }

        private void ProcessPacket(in IMPacket packet, in User user)
        {
            switch (packet)
            {
                case EntityDataTable entityDataTablePacket:
                    if (!_entityTable.ContainsKey(entityDataTablePacket.EntityInfo))
                    {
                        Console.WriteLine($"Error:: Not Contain EntityInfo {entityDataTablePacket.EntityInfo.EntityID}");
                        break;
                    }
                    _entityTable[entityDataTablePacket.EntityInfo].LoadDataTablePacket(entityDataTablePacket);
                    break;

                case GroundModificationPacket groundModificationPacket:
                    Vector3Int gPos = groundModificationPacket.Position;
                    Vector3Int gGridPos = gPos + new Vector3Int(ZoneOption.TotalZoneHalfSize);
                    EntityDataBase? gBlock = _groundEntities[gGridPos.X, gGridPos.Y, gGridPos.Z];
                    if (groundModificationPacket.IsCreate)
                    {
                        if (gBlock == null)
                        {
                            if (user.LinkedEntity is PlayerEntity player)
                            {
                                if (player.Inventory.GetItemCount(ItemCode.Block) <= 0)
                                {
                                    break;
                                }

                                if (!CreateEntity(EntityType.Block, out var entity, new Vector3(gPos.X, gPos.Y, gPos.Z)))
                                {
                                    Console.WriteLine($"Error:: Don't Create Entity [{EntityType.Block}]");
                                    break;
                                }
                                else
                                {
                                    player.Inventory.RemoveItem(ItemCode.Block, 1);
                                }
                            }
                        }
                    }
                    else
                    {
                        if (gBlock != null)
                        {
                            if (!ReleaseEntity(gBlock))
                            {
                                Console.WriteLine($"Error:: Don't Create Entity [{EntityType.Block}]");
                                break;
                            }
                            else
                            {
                                if (user.LinkedEntity is PlayerEntity player)
                                {
                                    if (player.Inventory.AddItem(ItemCode.Block, 1))
                                    {

                                    }
                                }
                            }
                        }
                    }
                    break;

                default:
                    Console.WriteLine("Error:: Not Found Packet Protocol!!");
                    break;
            }
        }

        private void RegisterConnectUser(User userInfo)
        {
            Console.WriteLine($"Log:: Connect User Register!!");

            lock (_connectUserSwapChain.CurrentBuffer)
            {
                _connectUserSwapChain.CurrentBuffer.Add(userInfo);
            }
        }

        private void RegisterDisconnectUser(User userInfo)
        {
            Console.WriteLine($"Log:: Disconnect User Register!!");

            lock (_disconnectUserSwapChain.CurrentBuffer)
            {
                _disconnectUserSwapChain.CurrentBuffer.Add(userInfo);
            }
        }

        private void ProcessConnectUser()
        {
            var connectUserList = _connectUserSwapChain.Swap();
            if (connectUserList.Count == 0)
            {
                return;
            }
            Console.WriteLine($"Log:: Process Connect Users!!");

            foreach (var user in connectUserList)
            {
                if (!_userIDDictionary.TryAdd(user.ID, user))
                {
                    Console.WriteLine($"Error:: Failed!! => _userIdDictionary.TryAdd({user.ID}, {user})");
                    continue;
                }
                else
                {
                    Console.WriteLine($"Log:: Success!! => _userIdDictionary.TryAdd({user.ID}, {user})");
                }
                _userList.Add(user);
                if (!CreateEntity(EntityType.Player, out var player, Vector3.Zero))
                {
                    Console.WriteLine($"Error:: Don't Create Entity [{EntityType.Block}]");
                    continue;
                }
                user.LinkEntity(player);
                //if (!_entityTable.TryAdd(user.LinkedEntity!.EntityInfo, user.LinkedEntity))
                //{
                //    Console.WriteLine($"Error:: Failed!! => _entityTable.TryAdd({user.LinkedEntity.EntityInfo}, {user.LinkedEntity})");
                //    continue;
                //}
                _setLinkedEntityPacket.EntityInfo = user.LinkedEntity!.EntityInfo;
                MemoryPackSerializer.Serialize<IMPacket, PacketBufferWriter>(user.PacketBufferWriter, _setLinkedEntityPacket);
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
            Console.WriteLine($"Log:: Process Disconnect Users!!");

            foreach (var user in disconnectUserList)
            {
                if (!_userIDDictionary.Remove(user.ID))
                {
                    Console.WriteLine($"Error:: Failed!! => _userIdDictionary.Remove({user.ID})");
                    continue;
                }
                else
                {
                    Console.WriteLine($"Log:: Success!! => _userIdDictionary.Remove({user.ID})");
                }
                _userList.Remove(user);
            }

            foreach (var user in disconnectUserList)
            {
                if (user.LinkedEntity != null)
                {
                    if (!_entityTable.Remove(user.LinkedEntity.EntityInfo))
                    {
                        Console.WriteLine($"Error:: Failed!! => _entityTable.Remove({user.LinkedEntity.EntityInfo.EntityID})");
                        continue;
                    }
                    else
                    {
                        Console.WriteLine($"Log:: Success!! => _entityTable.Remove({user.LinkedEntity.EntityInfo.EntityID})");
                    }
                }
                _userPool.Return(user);
                user.LinkedEntity?.CurrentZone.Value?.RemoveEntity(user.LinkedEntity);
            }
            disconnectUserList.Clear();
        }

        private bool CreateEntity(EntityType entityType, out EntityDataBase entity, Vector3 position)
        {
            switch (entityType)
            {
                case EntityType.Player:
                    entity = new PlayerEntity();
                    entity.Transform.Position = position;
                    break;
                case EntityType.Block:
                    entity = new BlockEntity();
                    entity.Transform.Position = position;
                    Vector3Int gPos = new Vector3Int(entity.Position.Value + ZoneOption.TotalZoneHalfSize);
                    _groundEntities[gPos.X, gPos.Y, gPos.Z] = entity;
                    break;

                default:
                    throw new NotImplementedException();
            }

            if (!_entityTable.TryAdd(entity.EntityInfo, entity))
            {
                Console.WriteLine($"Error:: Failed!! => _entityTable.TryAdd({entity.EntityInfo}, {entity})");
                return false;
            }

            return true;
        }

        private bool ReleaseEntity(EntityDataBase entity)
        {
            switch (entity.EntityType)
            {
                case EntityType.Player:
                    break;
                case EntityType.Block:
                    Vector3Int gPos = new Vector3Int(entity.Position.Value + ZoneOption.TotalZoneHalfSize);
                    _groundEntities[gPos.X, gPos.Y, gPos.Z] = null;
                    break;

                default:
                    throw new NotImplementedException();
            }

            if (!_entityTable.Remove(entity.EntityInfo))
            {
                Console.WriteLine($"Error:: Failed!! => _entityTable.TryAdd({entity.EntityInfo}, {entity})");
                return false;
            }

            entity.CurrentZone.Value?.RemoveEntity(entity);

            return true;
        }

        public Vector3 GetZonePosition(Vector3Int zoneCoord)
        {
            return new Vector3(zoneCoord.X, zoneCoord.Y, zoneCoord.Z) * ZoneOption.ZoneSize - (ZoneOption.ZoneCountXYZ - Vector3.One) * ZoneOption.ZoneSize * 0.5f;
        }

        public Zone GetZone(Vector3Int zoneCoord)
        {
            return _zones[zoneCoord.X, zoneCoord.Y, zoneCoord .Z];
        }

        private void SetZone(EntityDataBase entity)
        {
            Vector3 pos = entity.Position.Value;
            if (pos.X > ZoneOption.TotalZoneHalfWidth ||
                pos.Y > ZoneOption.TotalZoneHalfHeight ||
                pos.Z > ZoneOption.TotalZoneHalfDepth ||
                pos.X < -ZoneOption.TotalZoneHalfWidth ||
                pos.Y < -ZoneOption.TotalZoneHalfHeight ||
                pos.Z < -ZoneOption.TotalZoneHalfDepth)
            {
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

        private async Task LoadZoneDB(Vector3Int zoneCoord)
        {
            Zone zone = GetZone(zoneCoord);

            using (var response = await _httpClient.GetAsync($"Chunks?id={zone.ZoneID}"))
            {
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var jsonContent = await response.Content.ReadFromJsonAsync<ZoneDTO>();
                    ReadOnlyMemory<byte> buffer = Encoding.UTF8.GetBytes(jsonContent.ChunkBinary);
                    ZoneChunk zoneChunk = _zones[zoneCoord.X, zoneCoord.Y, zoneCoord.Z].ZoneChunk;
                    MemoryPackSerializer.Deserialize<ZoneChunk>(buffer.Span, ref zoneChunk!);
                    Vector3 zonePosition = GetZonePosition(zoneCoord);
                    for (int i = 0; i < ZoneOption.ZoneWidth; i++)
                    {
                        for (int j = 0; j < ZoneOption.ZoneHeight; j++)
                        {
                            for (int k = 0; k < ZoneOption.ZoneWidth; k++)
                            {
                                if (zoneChunk.chunks[i, j, k] == BlockType.Block)
                                {
                                    Vector3 blockPosition = zonePosition + new Vector3(i, j, k) - ZoneOption.ZoneSize * 0.5f + Vector3.One * 0.5f;
                                    if (!CreateEntity(EntityType.Block, out _, blockPosition))
                                    {
                                        Console.WriteLine($"Error:: Don't Create Entity [{EntityType.Block}]");
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    await CreateZoneDB(zoneCoord);
                }
            }
        }

        private async Task SaveZoneDB(Vector3Int zoneCoord)
        {
            Zone zone = GetZone(zoneCoord);

            using (var response = await _httpClient.PutAsJsonAsync($"Chunks?id={zone.ZoneID}", zone.GetZoneDTO()))
            {
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    //var jsonContent = await response.Content.ReadFromJsonAsync<ZoneDTO>();
                }
            }
        }

        private async Task CreateZoneDB(Vector3Int zoneCoord)
        {
            Zone zone = GetZone(zoneCoord);
            
            ZoneChunk zoneChunk = zone.ZoneChunk;

            if (zoneCoord.Y == 0)
            {
                for (int i = 0; i < ZoneOption.ZoneWidth; i++)
                {
                    for (int j = 0; j < ZoneOption.ZoneHeight; j++)
                    {
                        for (int k = 0; k < ZoneOption.ZoneWidth; k++)
                        {
                            zoneChunk.chunks[i, j, k] = BlockType.Block;
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < ZoneOption.ZoneWidth; i++)
                {
                    for (int j = 0; j < ZoneOption.ZoneHeight; j++)
                    {
                        for (int k = 0; k < ZoneOption.ZoneWidth; k++)
                        {
                            zoneChunk.chunks[i, j, k] = BlockType.None;
                        }
                    }
                }
            }

            using (var response = await _httpClient.PostAsJsonAsync($"Chunks", zone.GetZoneDTO()))
            {
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var jsonContent = await response.Content.ReadFromJsonAsync<ZoneDTO>();
                    ReadOnlyMemory<byte> buffer = Encoding.UTF8.GetBytes(jsonContent.ChunkBinary);
                    MemoryPackSerializer.Deserialize<ZoneChunk>(buffer.Span, ref zoneChunk!);
                    Vector3 zonePosition = GetZonePosition(zoneCoord);
                    for (int i = 0; i < ZoneOption.ZoneWidth; i++)
                    {
                        for (int j = 0; j < ZoneOption.ZoneHeight; j++)
                        {
                            for (int k = 0; k < ZoneOption.ZoneWidth; k++)
                            {
                                if (zoneChunk.chunks[i, j, k] == BlockType.Block)
                                {
                                    Vector3 blockPosition = zonePosition + new Vector3(i, j, k) - ZoneOption.ZoneSize * 0.5f + Vector3.One * 0.5f;
                                    if (!CreateEntity(EntityType.Block, out _, blockPosition))
                                    {
                                        Console.WriteLine($"Error:: Don't Create Entity [{EntityType.Block}]");
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
