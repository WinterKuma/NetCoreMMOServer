using MemoryPack;
using NetCoreMMOClient.Utility;
using NetCoreMMOServer.Packet;
using Sirenix.OdinInspector;
using System;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

public class Entity : MonoBehaviour
{
    public int NetObjectID = 1000;
    public float moveSpeed = 3.0f;
    public bool isMine = false;

    public Vector3 destinationPosition;

    // Start is called before the first frame update
    void Start()
    {
        if (isMine)
        {
            Camera.main.transform.parent = transform;
        }
        else
        {
            destinationPosition = transform.position;
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (isMine)
        {
            bool isMove = false;
            if (Input.GetKey(KeyCode.LeftArrow))
            {
                isMove = true;
                transform.position += Vector3.left * Time.fixedDeltaTime * moveSpeed;
            }
            if (Input.GetKey(KeyCode.RightArrow))
            {
                isMove = true;
                transform.position += Vector3.right * Time.fixedDeltaTime * moveSpeed;
            }
            if (Input.GetKey(KeyCode.UpArrow))
            {
                isMove = true;
                transform.position += Vector3.up * Time.fixedDeltaTime * moveSpeed;
            }
            if (Input.GetKey(KeyCode.DownArrow))
            {
                isMove = true;
                transform.position += Vector3.down * Time.fixedDeltaTime * moveSpeed;
            }
            if (isMove)
            {
                MoveDto dto = new MoveDto();
                dto.NetObjectID = NetObjectID;
                dto.Position = transform.position.ToSystemNumericsVector3();
                Main.instance.SendPacketMessage(MemoryPackSerializer.Serialize(dto.ToMPacket()));
            }
        }
        else
        {
            //transform.position = destinationPosition;

            if (Vector3.Distance(transform.position, destinationPosition) < moveSpeed * Time.fixedDeltaTime)
            {
                transform.position = destinationPosition;
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, destinationPosition, moveSpeed * Time.fixedDeltaTime);
            }
        }
    }

    public void OnDestroy()
    {
    }

    public void DtoReceived(Dto dto)
    {
        switch (dto)
        {
            case null:
                break;
            case EntityDto entity:
                gameObject.SetActive(entity.IsSpawn);
                Destroy(gameObject);
                transform.position = new Vector3(entity.Position.X, entity.Position.Y, entity.Position.Z);
                break;
            case MoveDto move:
                Vector3 movePosition = new Vector3(move.Position.X, move.Position.Y, move.Position.Z);
                if (isMine)
                {
                    if (Vector3.Distance(transform.position, movePosition) > 1.0f)
                    {
                        transform.position = movePosition;
                    }
                }
                else
                {
                    //Debug.Log($"Log:: {NetObjectID} Move");
                    if (Vector3.Distance(transform.position, movePosition) > moveSpeed)
                    {
                        transform.position = movePosition;
                        destinationPosition = movePosition;
                    }
                    else
                    {
                        destinationPosition = movePosition;
                    }
                }
                break;
            default:
                break;
        }
    }

    [Button]
    public void MoveEntity(Vector3Int dir)
    {
        transform.position = transform.position + dir;

        MoveDto dto = new MoveDto();
        dto.NetObjectID = NetObjectID;
        dto.Position = transform.position.ToSystemNumericsVector3();
        Main.instance.SendPacketMessage(MemoryPackSerializer.Serialize(dto.ToMPacket()));
    }

    [Button]
    public void SpawnEntity(bool isSpawn)
    {
        EntityDto dto = new EntityDto();
        dto.NetObjectID = NetObjectID;
        dto.IsSpawn = isSpawn;
        dto.Position = transform.position.ToSystemNumericsVector3();
        MPacket packet = new MPacket();
        dto.ToMPacket(ref packet);
        Main.instance.SendPacketMessage(MemoryPackSerializer.Serialize(dto));
    }
}
