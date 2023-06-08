using MemoryPack;
using NetCoreMMOServer.Network;
using NetCoreMMOServer.Packet;
using System.Collections.Generic;
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

        private EntityInfo _entityInfo;
        private EntityDataBase? _linkedEntity = null;
        //private List<EntityDataBase> _entities = new();
        private Dictionary<EntityInfo, EntityDataBase> _entityTable = new();

        public bool IsSpawn => _isSpawn;
        public int ClientID => _clientID;
        public int UserID => _userID;

        public DummyClient(int clientID)
        {
            _client = new();
            _client.Received += ProcessPacket;
            this._clientID = clientID;
        }

        public void Connect(IPEndPoint serverEP)
        {
            _client.OnConnectAsync(serverEP);
        }

        public void SetLinkEntity(EntityInfo entityInfo)
        {
            _entityInfo = entityInfo;
            if(_entityTable.ContainsKey(entityInfo))
            {
                _linkedEntity = _entityTable[entityInfo];
                _userID = (int)_linkedEntity.EntityID;
            }
        }

        private void ProcessPacket(IMPacket packet)
        {
            //2번
            switch (packet)
            {
                case null:
                    break;

                case SetLinkedEntityPacket setLinkedEntityPacket:
                    SetLinkEntity(setLinkedEntityPacket.EntityInfo);
                    break;

                case EntityDataTable entityDataTablePacket:
                    if(!_entityTable.ContainsKey(entityDataTablePacket.EntityInfo))
                    {
                        var entity = new EntityDataBase();
                        entity.Init(entityDataTablePacket.EntityInfo);
                        _entityTable.Add(entityDataTablePacket.EntityInfo, entity);
                        if(_linkedEntity == null)
                        {
                            SetLinkEntity(_entityInfo);
                        }
                    }
                    _entityTable[entityDataTablePacket.EntityInfo].LoadDataTablePacket(entityDataTablePacket);
                    if (!_entityTable[entityDataTablePacket.EntityInfo].IsActive.Value)
                    {
                        if (_entityTable.Remove(entityDataTablePacket.EntityInfo, out var entity))
                        {
                            break;
                        }
                    }
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
            PacketPool.ReturnPacket(packet);
        }

        public void Update(float dt)
        {
            if (_userID <= 0)
            {
                Console.WriteLine($"Error:: Not Ready Client => Update() (Client ID : {_clientID})");
                return;
            }

            if(_linkedEntity == null)
            {
                Console.WriteLine($"Error:: Not Linked Entity (Client ID : {_clientID})");
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
                _linkedEntity.Position.Value += _moveDir * dt * _moveSpeed;
                //Console.WriteLine($"Log:: Moving Client!! => isActive is true (Client ID : {clientID}, User ID : {userID})");
                _position += _moveDir * dt * _moveSpeed;
                MoveDto dto = new();
                dto.NetObjectID = _userID;
                dto.Position = _position;
                //MPacket packet = new MPacket();
                //dto.ToMPacket(ref packet);
                //ReadOnlyMemory<byte> buffer = packet.AsMemory();
                SendPacketMessage(MemoryPackSerializer.Serialize<IMPacket>(_linkedEntity.UpdateDataTablePacket()));
                _linkedEntity.ClearDataTablePacket();
            }
        }

        public async void SendPacketMessage(ReadOnlyMemory<byte> packet)
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
