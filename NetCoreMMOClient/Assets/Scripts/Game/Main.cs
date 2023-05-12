using NetCoreMMOServer.Network;
using NetCoreMMOServer.Packet;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class Main : MonoBehaviour
{
    private static Main _main = null;
    public static Main instance => _main;

    private Client _client;
    public Action<Dto> DtoReceived;

    private List<MPacket> _packets = new();
    private List<MPacket>[] _packetsBuffers = new List<MPacket>[] { new(), new() };
    private int _currentIndex = 0;

    private Dictionary<int, Entity> _entityDictionary = new();
    public GameObject entityPrefab;

    // Start is called before the first frame update
    void Awake()
    {
        //PacketReceived += PrintPacket;
        _main = this;

        _client = new();
        _client.Received += pushPacket;
        _client.OnConnect(new IPEndPoint(IPAddress.Loopback, 8080));
    }

    // Update is called once per frame
    void Update()
    {
        lock (_packetsBuffers[_currentIndex])
        {
            _packets = _packetsBuffers[_currentIndex];
            _currentIndex = _currentIndex == 0 ? 1 : 0;
        }
        foreach (var packet in _packets)
        {
            PrintPacket(packet);
            //DtoReceived?.Invoke(packet.Deserialize());
        }
        _packets.Clear();
    }

    private void OnDisable()
    {
        _client.Disconnect();
    }

    private void pushPacket(MPacket packet)
    {
        lock (_packetsBuffers[_currentIndex])
        {
            _packetsBuffers[_currentIndex].Add(packet);
        }
    }

    public void PrintPacket(MPacket packet)
    {
        Debug.Log(packet.PacketProtocol.ToString());

        Dto dto = packet.Deserialize();
        switch (dto)
        {
            case EntityDto entity:
                if (entity.IsSpawn)
                {
                    CreateEntity(entity);
                }
                else
                {
                    Destroy(_entityDictionary[entity.NetObjectID].gameObject);
                    _entityDictionary.Remove(entity.NetObjectID);
                }
                break;
            case MoveDto move:
                if (_entityDictionary.ContainsKey(move.NetObjectID))
                {
                    _entityDictionary[move.NetObjectID].DtoReceived(move);
                }
                else
                {
                    Console.WriteLine($"Error:: Not Found Entity[ID:{move.NetObjectID}]");
                }
                break;
            default:
                break;
        }
        //DtoReceived?.Invoke(dto);
    }

    [Button]
    public void CreateEntity(EntityDto dto)
    {
        Entity entity = Instantiate(entityPrefab, Vector3.zero, Quaternion.identity).GetComponent<Entity>();
        entity.isMine = dto.IsMine;
        entity.NetObjectID = dto.NetObjectID;
        entity.transform.position = new Vector3(dto.Position.X, dto.Position.Y, dto.Position.Z);
        _entityDictionary.Add(entity.NetObjectID, entity);
    }

    //[Button]
    //public void SendPacketMessage(string msg)
    //{
    //    msg += '\0';
    //    client.SendAsync(Encoding.UTF8.GetBytes(msg));
    //}

    [Button]
    public void SendPacketMessage(byte[] packet)
    {
        _ = _client.SendAsync(packet);
    }
}