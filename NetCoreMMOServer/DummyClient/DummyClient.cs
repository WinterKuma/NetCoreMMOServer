using MemoryPack;
using NetCoreMMOServer.Network;
using NetCoreMMOServer.Packet;
using System.Net;
using System.Numerics;

namespace DummyClient
{
    internal class DummyClient
    {
        private Client _client;
        private bool _isSpawn = false;
        private int _clientID = -1;
        private int _userID = 0;
        private Vector3 _position;

        private bool _isActive = true;
        private float _activeTime = 0.0f;
        private Vector3 _moveDir = Vector3.Zero;
        private float _moveSpeed = 3.0f;

        private Random _random = new Random();

        public bool IsSpawn => _isSpawn;
        public int ClientID => _clientID;
        public int UserID => _userID;

        public DummyClient(int clientID)
        {
            _client = new();
            _client.Received += ProcessPacket;
            this._clientID = clientID;
        }

        public void Connect()
        {
            _client.OnConnectAsync(new IPEndPoint(IPAddress.Loopback, 8080));
        }

        private void ProcessPacket(MPacket packet)
        {
            //2번
            var dto = packet.Deserialize();
            switch (dto)
            {
                case null:
                    break;
                case EntityDto entity:
                    if (!entity.IsMine)
                    {
                        break;
                    }
                    if (_userID > 0 && entity.NetObjectID != _userID) break;
                    _userID = entity.NetObjectID;
                    _isSpawn = entity.IsSpawn;
                    _position = entity.Position;
                    Console.WriteLine($"ID[{entity.NetObjectID}]");
                    break;
                case MoveDto move:
                    if (move.NetObjectID != _userID) break;
                    if (Vector3.Distance(_position, move.Position) > 1.0f)
                    {
                        _position = move.Position;
                    }
                    //Console.WriteLine($"ID[{move.NetObjectID}] : Position {move?.Position}");
                    break;
                default:
                    break;
            }
        }

        public void Update(float dt)
        {
            if (_userID <= 0)
            {
                Console.WriteLine($"Error:: Not Ready Client => Update() (Client ID : {_clientID})");
                return;
            }

            _activeTime -= dt;
            if (_activeTime <= 0.0f)
            {
                _activeTime = _random.Next(1, 4);
                _isActive = true;
                _moveDir = _random.Next(0, 8) switch
                {
                    0 => new Vector3(0.0f, -1.0f, 0.0f),
                    1 => new Vector3(1.0f, -1.0f, 0.0f),
                    2 => new Vector3(1.0f, 0.0f, 0.0f),
                    3 => new Vector3(1.0f, 1.0f, 0.0f),
                    4 => new Vector3(0.0f, 1.0f, 0.0f),
                    5 => new Vector3(-1.0f, 1.0f, 0.0f),
                    6 => new Vector3(-1.0f, 0.0f, 0.0f),
                    7 => new Vector3(-1.0f, -1.0f, 0.0f),
                    _ => _moveDir,
                };
                _moveDir = _moveDir / _moveDir.Length();
            }

            if (_isActive)
            {
                //Console.WriteLine($"Log:: Moving Client!! => isActive is true (Client ID : {clientID}, User ID : {userID})");
                _position += _moveDir * dt * _moveSpeed;
                MoveDto dto = new();
                dto.NetObjectID = _userID;
                dto.Position = _position;
                SendPacketMessage(MemoryPackSerializer.Serialize(dto.Serialize()));
            }
        }

        public async void SendPacketMessage(byte[] packet)
        {
            if (_client is null)
            {
                return;
            }

            await _client.SendAsync(packet);
        }

        public void Disconnect()
        {
            _client.Disconnect();
        }
    }
}
