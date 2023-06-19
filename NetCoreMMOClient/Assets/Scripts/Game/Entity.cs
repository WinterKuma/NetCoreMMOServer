using MemoryPack;
using NetCoreMMOClient.Utility;
using NetCoreMMOServer.Network;
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

    public EntityDataBase EntityData { get; set; } = null;

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
            if (EntityData.Position.IsDirty)
            {
                if (Vector3.Distance(transform.position, EntityData.Position.Value) > _moveSpeed * 0.5f)
                {
                    transform.position = EntityData.Position.Value;
                }
                EntityData.Position.IsDirty = false;
            }

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
                transform.position = transform.position + dir.normalized * _moveSpeed * Time.fixedDeltaTime;
                EntityData.Position.Value = transform.position;
                //MoveEntity(dir);
            }

            Main.Instance.SendPacketMessage(MemoryPackSerializer.Serialize<IMPacket>(EntityData.UpdateDataTablePacket()));
            EntityData.ClearDataTablePacket();
        }
        else
        {
            //transform.position = destinationPosition;
            if(EntityData.Position.IsDirty)
            {
                _destinationPosition = EntityData.Position.Value;
            }
            if (Vector3.Distance(transform.position, _destinationPosition) < _moveSpeed * Time.fixedDeltaTime)
            {
                transform.position = _destinationPosition;
            }
            else if (Vector3.Distance(transform.position, _destinationPosition) < _moveSpeed * 0.5f)
            {
                //transform.position = Vector3.MoveTowards(transform.position, _destinationPosition, _moveSpeed * Time.deltaTime);
                transform.position = Vector3.Lerp(transform.position, _destinationPosition, _moveSpeed * Time.fixedDeltaTime);
            }
            else
            {
                transform.position = _destinationPosition;
            }
        }
    }

    public void OnDestroy()
    {
    }
}
