using MemoryPack;
using NetCoreMMOClient.Utility;
using NetCoreMMOServer.Packet;
using Sirenix.OdinInspector;
using UnityEngine;

public class Entity : MonoBehaviour
{
    [field: SerializeField]
    public int NetObjectID { get; set; } = 1000;
    [field: SerializeField]
    public bool IsMine { get; set; } = false;
    [field: SerializeField]
    private float _moveSpeed = 3.0f;

    private Vector3 _destinationPosition;

    // Start is called before the first frame update
    void Start()
    {
        if (IsMine)
        {
            Camera.main.transform.parent = transform;
        }
        else
        {
            _destinationPosition = transform.position;
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (IsMine)
        {
            bool isMove = false;
            Vector3 dir = Vector3.zero;
            if (Input.GetKey(KeyCode.LeftArrow))
            {
                isMove = true;
                dir += Vector3.left;
            }
            if (Input.GetKey(KeyCode.RightArrow))
            {
                isMove = true;
                dir += Vector3.right;
            }
            if (Input.GetKey(KeyCode.UpArrow))
            {
                isMove = true;
                dir += Vector3.up;
            }
            if (Input.GetKey(KeyCode.DownArrow))
            {
                isMove = true;
                dir += Vector3.down;
            }
            if (isMove)
            {
                MoveEntity(dir);
            }
        }
        else
        {
            //transform.position = destinationPosition;

            if (Vector3.Distance(transform.position, _destinationPosition) < _moveSpeed * Time.fixedDeltaTime)
            {
                transform.position = _destinationPosition;
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, _destinationPosition, _moveSpeed * Time.fixedDeltaTime);
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
                if (IsMine)
                {
                    if (Vector3.Distance(transform.position, movePosition) > 1.0f)
                    {
                        transform.position = movePosition;
                    }
                }
                else
                {
                    //Debug.Log($"Log:: {NetObjectID} Move");
                    if (Vector3.Distance(transform.position, movePosition) > _moveSpeed)
                    {
                        transform.position = movePosition;
                        _destinationPosition = movePosition;
                    }
                    else
                    {
                        _destinationPosition = movePosition;
                    }
                }
                break;
            default:
                break;
        }
    }

    [Button]
    public void MoveEntity(Vector3 dir)
    {
        transform.position = transform.position + dir.normalized * _moveSpeed * Time.fixedDeltaTime;

        MoveDto dto = new MoveDto();
        dto.NetObjectID = NetObjectID;
        dto.Position = transform.position.ToSystemNumericsVector3();
        Main.Instance.SendPacketMessage(MemoryPackSerializer.Serialize(dto.ToMPacket()));
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
        Main.Instance.SendPacketMessage(MemoryPackSerializer.Serialize(dto));
    }
}
