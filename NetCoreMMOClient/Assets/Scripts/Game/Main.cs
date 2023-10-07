using MemoryPack;
using NetCoreMMOServer.Network;
using NetCoreMMOServer.Packet;
using NetCoreMMOServer.Utility;
using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class Main : MonoBehaviour
{
    private static Main _main = null;
    public static Main Instance => _main;

    private Client _client;

    private SwapChain<List<IMPacket>> _packetBufferSwapChain = new();

    private Dictionary<EntityInfo, Entity> _entityDictionary = new();
    public GameObject EntityPrefab;
    public GameObject GroundPrefab;

    private EntityInfo _entityInfo;
    private Entity? _linkedEntity = null;

    private static string ip = "127.0.0.1";
    private static int port = 8080;

    //UI
    [SerializeField]
    private UIInventory _uiInventory;
    [SerializeField]
    private UIMyHp _uiMyHp;

    // Start is called before the first frame update
    void Awake()
    {
        _main = this;

        _client = new();
        _client.Received += pushPacket;
        _client.OnConnect(new IPEndPoint(IPAddress.Parse(ip), port));

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Update is called once per frame
    void Update()
    {
        var packetBuffer = _packetBufferSwapChain.Swap();
        foreach (var packet in packetBuffer)
        {
            PrintPacket(packet);
        }
        packetBuffer.Clear();

        //if (_linkedEntity != null)
        //{
        //    SendPacketMessage(MemoryPackSerializer.Serialize<IMPacket>(_linkedEntity.EntityData.UpdateDataTablePacket()));
        //    _linkedEntity.EntityData.ClearDataTablePacket();
        //}
    }

    private void OnDisable()
    {
        _client.Disconnect();
    }

    private void pushPacket(IMPacket packet)
    {
        lock (_packetBufferSwapChain.CurrentBuffer)
        {
            _packetBufferSwapChain.CurrentBuffer.Add(packet);
        }
    }

    public void SetLinkEntity(EntityInfo entityInfo)
    {
        _entityInfo = entityInfo;
        if (_entityDictionary.ContainsKey(entityInfo))
        {
            _linkedEntity = _entityDictionary[entityInfo];
            _uiInventory.SetPlayerEntity(_linkedEntity);
            _uiMyHp.SetPlayerEntity(_linkedEntity);
        }
    }

    public void PrintPacket(IMPacket packet)
    {
        switch (packet)
        {
            case SetLinkedEntityPacket setLinkedEntityPacket:
                SetLinkEntity(setLinkedEntityPacket.EntityInfo);
                break;

            case EntityDataTable entityDataTablePacket:
                var entityInfo = entityDataTablePacket.EntityInfo;
                if (!_entityDictionary.ContainsKey(entityInfo))
                {
                    _entityDictionary.Add(entityInfo, CreateEntity(entityDataTablePacket));
                    if (_linkedEntity == null)
                    {
                        SetLinkEntity(_entityInfo);
                    }
                }
                _entityDictionary[entityInfo].EntityData.LoadDataTablePacket(entityDataTablePacket);
                if (!_entityDictionary[entityInfo].EntityData.IsActive.Value)
                {
                    if (_entityDictionary.Remove(entityInfo, out var entity))
                    {
                        Destroy(entity.gameObject);
                        break;
                    }
                }
                break;

            default:
                break;
        }
    }

    public Entity CreateEntity(EntityDataTable entityDataTable)
    {
        GameObject obj = null;
        switch (entityDataTable.EntityInfo.EntityType)
        {
            case EntityType.Player:
                obj = EntityPrefab;
                break;
            case EntityType.Block:
                obj = GroundPrefab;
                break;

            default:
                Debug.LogError($"Not Found EntityType");
                throw new Exception();
        }
        Entity entity = Instantiate(obj, Vector3.zero, Quaternion.identity).GetComponent<Entity>();
        entity.IsMine = _entityInfo.EntityID == entityDataTable.EntityInfo.EntityID;
        entity.NetObjectID = (int)entityDataTable.EntityInfo.EntityID;

        switch (entityDataTable.EntityInfo.EntityType)
        {
            case EntityType.Player:
                entity.EntityData = new PlayerEntity();
                break;
            case EntityType.Block:
                entity.EntityData = new EntityDataBase();
                break;

            default:
                throw new Exception();
        }
        entity.EntityData.Init(entityDataTable.EntityInfo);
        return entity;
    }

    public void SendPacketMessage(byte[] packet)
    {
        _ = _client.SendAsync(packet);
    }
}