﻿using MemoryPack;
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

        private SwapChain<Queue<(IMPacket, User)>> _packetQueueSwapChain;
        //private Queue<IMPacket> _broadcastPacketQueue;

        private ConcurrentPool<PacketBufferWriter> _rpcPacketBufferWriterPool;

        private List<Zone> _zoneList;
        private Zone[,,] _zones;
        private Dictionary<EntityInfo, EntityDataBase> _entityTable;
        //private ConcurrentPool<EntityDataBase> _entityDataBasePool;

        //Packet Instance
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
            //_broadcastPacketQueue = new();

            _zoneList = new List<Zone>(ZoneOption.ZoneCountX * ZoneOption.ZoneCountY * ZoneOption.ZoneCountZ);
            _zones = new Zone[ZoneOption.ZoneCountX, ZoneOption.ZoneCountY, ZoneOption.ZoneCountZ];
            for(int x = 0; x < ZoneOption.ZoneCountX; ++x)
            {
                for(int y = 0; y < ZoneOption.ZoneCountY; ++y)
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
                        packet.Item2?.LinkedEntity?.CurrentZone.Value?.PacketQueue.Enqueue(packet);
                        //ProcessPacket(packet.Item1);
                        //PacketPool.ReturnPacket(packet.Item1);
                    }
                }

                // Update Zone Threading
                Parallel.ForEach(_zoneList,
                    zone =>
                    {
                        while (zone.PacketQueue.Count > 0)
                        {
                            if (zone.PacketQueue.TryDequeue(out var packet))
                            {
                                ProcessPacket(packet.Item1);
                                PacketPool.ReturnPacket(packet.Item1);
                            }
                        }
                    });

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

                // Update Disconnect User
                ProcessDisconnectUser();

                // Update Zone (with. Entity)
                foreach(var entity in _entityTable.Values)
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
                foreach(var zone in _zones)
                {
                    zone.ResetAndBackupEntityList();
                }
                
                // Sleep MainLoop Thread
                if (st.ElapsedMilliseconds < 200)
                {
                    //Thread.Sleep(1);
                    Console.WriteLine($"Log:: ElapsedMilliseconds : {st.ElapsedMilliseconds}");
                    Thread.Sleep(Math.Max(0, (int)(200 - st.ElapsedMilliseconds)));
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

        private void ProcessPacket(in IMPacket packet)
        {
            switch (packet)
            {
                case EntityDataTable entityDataTablePacket:
                    if(!_entityTable.ContainsKey(entityDataTablePacket.EntityInfo))
                    {
                        Console.WriteLine($"Error:: Not Contain EntityInfo {entityDataTablePacket.EntityInfo.EntityID}");
                        break;
                    }
                    _entityTable[entityDataTablePacket.EntityInfo].LoadDataTablePacket(entityDataTablePacket);
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
                user.LinkEntity(new EntityDataBase());
                if (!_entityTable.TryAdd(user.LinkedEntity!.EntityInfo, user.LinkedEntity))
                {
                    Console.WriteLine($"Error:: Failed!! => _entityTable.TryAdd({user.LinkedEntity.EntityInfo}, {user.LinkedEntity})");
                    continue;
                }
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
                    if(!_entityTable.Remove(user.LinkedEntity.EntityInfo))
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
                //entity.MoveZone(_zones[1, 1]);
                //return;
                pos = entity.Position.Value;
            }

            int x = (int)((pos.X + ZoneOption.TotalZoneHalfWidth) * ZoneOption.InverseZoneWidth);
            int y = (int)((pos.Y + ZoneOption.TotalZoneHalfHeight) * ZoneOption.InverseZoneHeight);
            int z = (int)((pos.Z + ZoneOption.TotalZoneHalfDepth) * ZoneOption.InverseZoneDepth);
            entity.MoveZone(_zones[x, y, z]);
        }
    }
}
