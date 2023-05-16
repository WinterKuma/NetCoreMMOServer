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

        private SwapChain<List<UserInfo>> _connectUserInfoSwapChain;
        private SwapChain<List<UserInfo>> _disconnectUserInfoSwapChain;
        private List<User> _userList;
        private readonly int _port;

        private ConcurrentPool<UserInfo> _userInfoPool;
        private Dictionary<int, UserInfo> _userIdDictionary;

        private SwapChain<Queue<IMPacket>> _packetQueueSwapChain;
        private Queue<IMPacket> _broadcastPacketQueue;

        private PacketBufferWriter _packetBufferWriter;

        public MMOServer(int port)
        {
            _port = port;

            _userList = new List<User>();
            _connectUserInfoSwapChain = new();
            _disconnectUserInfoSwapChain = new();

            _userInfoPool = new();
            _userIdDictionary = new Dictionary<int, UserInfo>();

            _packetQueueSwapChain = new();
            _broadcastPacketQueue = new();

            _packetBufferWriter = new PacketBufferWriter(new byte[1 << 16]);
        }

        public void Start(int backlog = (int)SocketOptionName.MaxConnections)
        {
            _server = new Server(_port);
            _server.Accepted += AcceptAsync;
            _server.Start(backlog);
            Update();
        }

        private void Update()
        {
            Stopwatch st = new Stopwatch();
            st.Start();
            while (true)
            {
                long deltaMilliseconds = st.ElapsedMilliseconds;
                float dt = deltaMilliseconds / 1000.0f;
                st.Restart();

                // Update Connect User;
                ProcessConnectUser();

                // Update Packet
                var packetQueue = _packetQueueSwapChain.Swap();
                while (packetQueue.Count > 0)
                {
                    if (packetQueue.TryDequeue(out var packet))
                    {
                        ProcessPacket(packet);
                    }
                }

                // Update Disconnect User
                ProcessDisconnectUser();

                // Send Packet

                if (_broadcastPacketQueue.Count > 0)
                {
                    _packetBufferWriter.Clear();
                    while (_broadcastPacketQueue.Count > 0)
                    {
                        if (_broadcastPacketQueue.TryDequeue(out var packet))
                        {
                            MemoryPackSerializer.Serialize(_packetBufferWriter, packet);
                        }
                    }
                    SendBroadcast(_packetBufferWriter.GetFilledMemory());
                }

                //Console.WriteLine($"Log:: Loop Tick ElapsedMilliseconds : {st.ElapsedMilliseconds}");
                // Sleep MainLoop Thread
                if (st.ElapsedMilliseconds < 33)
                {
                    Thread.Sleep(1);
                    //Thread.Sleep(Math.Max(0, (int)(33 - st.ElapsedMilliseconds)));
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

            var client = new User(socket);

            UserInfo userInfo = _userInfoPool.Get();
            userInfo.User = client;
            RegisterConnectUser(userInfo);

            try
            {
                await Task.WhenAll(ReceiveAsync(client));
                await client.CompleteAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            RegisterDisconnectUser(userInfo);

            Console.WriteLine($"Log::Socket[{socket.RemoteEndPoint}]: disconnected");
            socket.Disconnect(false);
            socket.Close();
        }

        private async ValueTask SendAsync(User client, ReadOnlyMemory<byte> buffer)
        {
            _ = await client.Writer.WriteAsync(buffer).ConfigureAwait(false);
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
                case null:
                    break;
                case EntityDto:
                    Console.WriteLine("Error:: Bug Packet");
                    break;
                case MoveDto move:
                    if (move.Position.Length() >= 30.0f)
                    {
                        move.Position = Vector3.Zero;
                    }
                    if (_userIdDictionary.ContainsKey(move.NetObjectID))
                    {
                        _userIdDictionary[move.NetObjectID].Position = move.Position;
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

        private void RegisterConnectUser(UserInfo userInfo)
        {
            Console.WriteLine($"Log:: Connect User Register!!");

            lock (_connectUserInfoSwapChain.CurrentBuffer)
            {
                _connectUserInfoSwapChain.CurrentBuffer.Add(userInfo);
            }
        }

        private void RegisterDisconnectUser(UserInfo userInfo)
        {
            Console.WriteLine($"Log:: Disconnect User Register!!");

            lock (_disconnectUserInfoSwapChain.CurrentBuffer)
            {
                _disconnectUserInfoSwapChain.CurrentBuffer.Add(userInfo);
            }
        }

        private void ProcessConnectUser()
        {
            var connectUserInfoList = _connectUserInfoSwapChain.Swap();
            if (connectUserInfoList.Count == 0)
            {
                return;
            }
            Console.WriteLine($"Log:: Process Connect Users!!");

            foreach (var userInfo in connectUserInfoList)
            {
                if (!_userIdDictionary.TryAdd(userInfo.Id, userInfo))
                {
                    Console.WriteLine($"Error:: Failed!! => _userIdDictionary.TryAdd({userInfo.Id}, {userInfo})");
                    continue;
                }
                else
                {
                    Console.WriteLine($"Log:: Success!! => _userIdDictionary.TryAdd({userInfo.Id}, {userInfo})");
                }
                _userList.Add(userInfo.User);
            }

            foreach (var userInfo in connectUserInfoList)
            {
                EnterUser(userInfo.User, userInfo.Id);
            }
            connectUserInfoList.Clear();
        }

        private void ProcessDisconnectUser()
        {
            var disconnectUserInfoList = _disconnectUserInfoSwapChain.Swap();
            if (disconnectUserInfoList.Count == 0)
            {
                return;
            }
            Console.WriteLine($"Log:: Process Disconnect Users!!");

            foreach (var userInfo in disconnectUserInfoList)
            {
                if (!_userIdDictionary.Remove(userInfo.Id))
                {
                    Console.WriteLine($"Error:: Failed!! => _userIdDictionary.Remove({userInfo.Id})");
                    continue;
                }
                else
                {
                    Console.WriteLine($"Log:: Success!! => _userIdDictionary.Remove({userInfo.Id})");
                }
                _userList.Remove(userInfo.User);
                _userInfoPool.Return(userInfo);
            }

            foreach (var userInfo in disconnectUserInfoList)
            {
                ExitUser(userInfo.Id);
            }
            disconnectUserInfoList.Clear();
        }

        private void EnterUser(User client, int userId)
        {
            Console.WriteLine($"[{userId}]User Enter!!");

            EntityDto entityDto = new EntityDto();
            entityDto.NetObjectID = userId;
            entityDto.IsMine = true;
            entityDto.IsSpawn = true;
            entityDto.Position = new Vector3(0, 0, 0);
            _ = SendAsync(client, MemoryPackSerializer.Serialize<IMPacket>(entityDto));
            entityDto.IsMine = false;
            SendAnother(client, MemoryPackSerializer.Serialize<IMPacket>(entityDto));
            foreach (var user in _userIdDictionary.Values)
            {
                if (user.Id != userId)
                {
                    entityDto.NetObjectID = user.Id;
                    entityDto.IsMine = false;
                    entityDto.IsSpawn = true;
                    entityDto.Position = user.Position;
                    _ = SendAsync(client, MemoryPackSerializer.Serialize<IMPacket>(entityDto));
                }
            }
        }

        private void ExitUser(int userId)
        {
            Console.WriteLine($"[{userId}] User Exited...");

            try
            {
                EntityDto entityDto = new EntityDto();
                entityDto.NetObjectID = userId;
                entityDto.IsSpawn = false;
                SendBroadcast(MemoryPackSerializer.Serialize<IMPacket>(entityDto));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
