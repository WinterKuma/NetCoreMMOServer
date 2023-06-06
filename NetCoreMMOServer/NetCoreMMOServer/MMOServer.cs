using MemoryPack;
using NetCoreMMOServer.Network;
using NetCoreMMOServer.Packet;
using NetCoreMMOServer.Utility;
using System;
using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Numerics;

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

        private SwapChain<Queue<IMPacket>> _packetQueueSwapChain;
        private Queue<IMPacket> _broadcastPacketQueue;

        private PacketBufferWriter _broadcastPacketBufferWriter;
        private PacketBufferWriter _userEventPacketBufferWriter;
        private ConcurrentPool<PacketBufferWriter> _rpcPacketBufferWriterPool;

        private Zone _zone;
        private Dictionary<EntityInfo, EntityDataBase> _entityTable;
        //private ConcurrentPool<EntityDataBase> _entityDataBasePool;
        //Packet Instance
        private EntityDto _entityDto;
        private SetLinkedEntityPacket _setLinkedEntityPacket;

        public MMOServer(int port)
        {
            _port = port;

            _userList = new();
            _connectUserSwapChain = new();
            _disconnectUserSwapChain = new();

            _userPool = new();
            _userIDDictionary = new();

            _packetQueueSwapChain = new();
            _broadcastPacketQueue = new();

            _broadcastPacketBufferWriter = new(new byte[0xffffff]);
            _userEventPacketBufferWriter = new(new byte[0xffffff]);

            _zone = new Zone();
            _entityTable = new();

            _entityDto = new EntityDto();
            _setLinkedEntityPacket = new();
        }

        public void Start(int backlog = (int)SocketOptionName.MaxConnections)
        {
            _server = new(_port);
            _server.Accepted += AcceptAsync;
            _server.Start(backlog);
            Update();
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
                        ProcessPacket(packet);
                        PacketPool.ReturnPacket(packet);
                    }
                }

                // Update Disconnect User
                ProcessDisconnectUser();

                // Update Zone (with. Entity)

                // Update User & Send Packet
                foreach (var user in _userList)
                {
                    user.WritePacket();
                    SendAsync(user, user.PacketBufferWriter.GetFilledMemory());
                }

                // Reset And Backup Zone EntityList
                _zone.ResetAndBackupEntityList();

                // Send Packet

                //foreach (var user in _userIDDictionary.Values)
                //{
                //    user.WritePacket();
                //    SendAsync(user, user.PacketBufferWriter.GetFilledMemory());
                //}

                //if (_broadcastPacketQueue.Count > 0)
                //{
                //    _broadcastPacketBufferWriter.Clear();
                //    while (_broadcastPacketQueue.Count > 0)
                //    {
                //        if (_broadcastPacketQueue.TryDequeue(out var packet))
                //        {
                //            MemoryPackSerializer.Serialize(_broadcastPacketBufferWriter, packet);
                //        }
                //    }
                //    SendBroadcast(_broadcastPacketBufferWriter.GetFilledMemory());
                //}

                //Console.WriteLine($"Log:: Loop Tick ElapsedMilliseconds : {st.ElapsedMilliseconds}");
                // Sleep MainLoop Thread
                if (st.ElapsedMilliseconds < 200)
                {
                    //Thread.Sleep(1);
                    Thread.Sleep(Math.Max(0, (int)(200 - st.ElapsedMilliseconds)));
                }
                else
                {
                    Console.WriteLine($"Log:: Hight Tick with GC ElapsedMilliseconds : {st.ElapsedMilliseconds}");
                }
            }
        }

        private async ValueTask AcceptAsync(Socket socket)
        {
            Console.WriteLine($"Log::Socket[{socket.RemoteEndPoint}]: connected");

            var client = _userPool.Get();
            client.Init(socket);
            client.LinkEntity(new EntityDataBase());
            _entityTable.TryAdd(client.LinkedEntity!.EntityInfo, client.LinkedEntity);
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
                            _packetQueueSwapChain.CurrentBuffer.Enqueue(packet);
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

        private void ProcessPacket(in IMPacket packet)
        {
            switch (packet)
            {
                case EntityDataTable entityDataTablePacket:
                    if(!_entityTable.ContainsKey(entityDataTablePacket.EntityInfo))
                    {
                        Console.WriteLine("Error:: Not Contain EntityInfo");
                        break;
                    }
                    _entityTable[entityDataTablePacket.EntityInfo].LoadDataTablePacket(entityDataTablePacket);
                    break;

                case EntityDto entity:
                    Console.WriteLine("Error:: Bug Packet");
                    break;
                case MoveDto move:
                    if (move.Position.Length() >= 30.0f)
                    {
                        move.Position = Vector3.Zero;
                    }
                    if (_userIDDictionary.ContainsKey(move.NetObjectID))
                    {
                        //_userIdDictionary[move.NetObjectID].Position = move.Position;
                        _broadcastPacketQueue.Enqueue(move);
                    }
                    else
                    {
                        Console.WriteLine($"Error::Not Found NetObject[ID:{move.NetObjectID}]");
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
                _setLinkedEntityPacket.EntityInfo = user.LinkedEntity!.EntityInfo;
                //var rpcPacket = _rpcPacketBufferWriterPool.Get();
                MemoryPackSerializer.Serialize<IMPacket, PacketBufferWriter>(user.PacketBufferWriter, _setLinkedEntityPacket);
                //SendRPCAsync(user, rpcPacket);
            }

            _userEventPacketBufferWriter.Clear();
            foreach (var user in connectUserList)
            {
                user.AddZone(_zone);
                //_zone.AddEntity(user.LinkedEntity);
                //EnterUser(user, user.ID);
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
                _userPool.Return(user);
            }

            _userEventPacketBufferWriter.Clear();
            foreach (var user in disconnectUserList)
            {
                user.RemoveZone(_zone);

                if (user.LinkedEntity != null)
                {
                    _entityTable.Remove(user.LinkedEntity.EntityInfo);
                }
                //_zone.RemoveEntity(user.LinkedEntity);
                //ExitUser(user.ID);
            }
            disconnectUserList.Clear();
        }

        private void EnterUser(User client, int userId)
        {
            Console.WriteLine($"[{userId}]User Enter!!");

            _entityDto.NetObjectID = userId;
            _entityDto.IsMine = false;
            _entityDto.IsSpawn = true;
            _entityDto.Position = new Vector3(0, 0, 0);

            MemoryPackSerializer.Serialize<IMPacket, PacketBufferWriter>(_userEventPacketBufferWriter, _entityDto);
            SendAnother(client, _userEventPacketBufferWriter.GetFilledMemory());

            _entityDto.IsMine = true;
            MemoryPackSerializer.Serialize<IMPacket, PacketBufferWriter>(_userEventPacketBufferWriter, _entityDto);
            _ = SendAsync(client, _userEventPacketBufferWriter.GetFilledMemory());
            //_ = SendAsync(client, MemoryPackSerializer.Serialize<IMPacket>(entityDto));
            foreach (var user in _userIDDictionary.Values)
            {
                if (user.ID != userId)
                {
                    _entityDto.NetObjectID = user.ID;
                    _entityDto.IsMine = false;
                    _entityDto.IsSpawn = true;
                    //_entityDto.Position = user.Position;
                    MemoryPackSerializer.Serialize<IMPacket, PacketBufferWriter>(_userEventPacketBufferWriter, _entityDto);
                    _ = SendAsync(client, _userEventPacketBufferWriter.GetFilledMemory());
                }
            }
            //_ = SendAsync(client, _userEventPacketBufferWriter.GetFilledMemory());
        }

        private void ExitUser(int userId)
        {
            Console.WriteLine($"[{userId}] User Exited...");

            try
            {
                _entityDto.NetObjectID = userId;
                _entityDto.IsSpawn = false;
                MemoryPackSerializer.Serialize<IMPacket, PacketBufferWriter>(_userEventPacketBufferWriter, _entityDto);
                SendBroadcast(_userEventPacketBufferWriter.GetFilledMemory());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
