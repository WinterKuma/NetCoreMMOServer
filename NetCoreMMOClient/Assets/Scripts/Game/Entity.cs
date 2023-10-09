using MemoryPack;
using NetCoreMMOServer.Network;
using NetCoreMMOServer.Packet;
using System.Collections.Generic;
using UnityEngine;

public class Entity : MonoBehaviour
{
    [field: SerializeField]
    public int NetObjectID { get; set; } = 1000;
    [field: SerializeField]
    public bool IsMine { get; set; } = false;
    [field: SerializeField]
    private float _moveSpeed = 3.0f;
    [field: SerializeField]
    private float _jumpPower = 6.0f;

    public EntityDataBase EntityData { get; set; } = null;

    private (Vector3 position, float time) _destinationInfo;
    private Vector3 _lastPosition;

    private float _moveTimer = 0.0f;
    [field: SerializeField]
    private float _jumpTimer = 0.0f;
    private readonly float _jumpDelay = 0.8f;
    private float _destPositionQueuingTimer = 0.0f;
    private Queue<(Vector3 position, float time)> _destPositionQueue = new();


    private bool _isStop = false;

    [field: SerializeField]
    private MeshRenderer _renderer;

    [field: SerializeField]
    private Material _mineMaterial;

    [field: SerializeField]
    private float _screenRotateSpeedX = 10.0f;
    [field: SerializeField]
    private float _screenRotateSpeedY = 10.0f;

    private float _xRotate = 0.0f;
    private float _yRotate = 0.0f;

    private int _groundBoxLayerMask = 0;

    private GameObject _hitGroundBox;
    private Vector3 _hitNormal;
    private GroundModificationPacket _groundModificationPacket;

    [field: SerializeField]
    private Collider _collider;

    // Start is called before the first frame update
    void Start()
    {
        if (IsMine)
        {
            Camera.main.transform.parent = transform;
            _renderer.material = _mineMaterial;
            _groundBoxLayerMask = 1 << LayerMask.NameToLayer("GroundBox");
            _groundModificationPacket = new GroundModificationPacket();
            _collider.enabled = false;
        }
        else
        {
            //Init();
        }
    }

    public void Init()
    {
        _lastPosition = transform.position;
        _destinationInfo = (transform.position, 1.0f);
    }

    // Update is called once per frame
    void Update()
    {
        MovementUpdate();

        if (IsMine)
        {
            ScreenRotationUpdate();
            MoveUpdate();
            InventoryUpdate();
            MouseUpdate();
            EntityPacketUpdate();
        }
    }

    public void ScreenRotationUpdate()
    {
        float xRotateMove = -Input.GetAxis("Mouse Y") * _screenRotateSpeedY;
        float yRotateMove = Input.GetAxis("Mouse X") * _screenRotateSpeedX;

        _yRotate = _yRotate + yRotateMove;
        _xRotate = _xRotate + xRotateMove;
        Debug.Log($"Y: {_yRotate}, X: {_xRotate}");
        _xRotate = Mathf.Clamp(_xRotate, -90f, 90f);
        transform.rotation = Quaternion.Euler(new Vector3(0f, _yRotate, 0f));
        EntityData.Rotation.Value = transform.rotation;

        Camera.main.transform.localRotation = Quaternion.Euler(new Vector3(_xRotate, 0f, 0f));
    }

    public void MoveUpdate()
    {
        bool isJump = false;
        Vector3 dir = Vector3.zero;
        if (Input.GetKey(KeyCode.A))
        {
            dir += Vector3.left;
        }
        if (Input.GetKey(KeyCode.D))
        {
            dir += Vector3.right;
        }
        if (Input.GetKey(KeyCode.W))
        {
            dir += Vector3.forward;
        }
        if (Input.GetKey(KeyCode.S))
        {
            dir += Vector3.back;
        }
        if (Input.GetKey(KeyCode.Space))
        {
            if (_jumpTimer >= _jumpDelay)
            {
                _jumpTimer = 0.0f;
                if (EntityData is PlayerEntity playerEntity)
                {
                    playerEntity.IsJump.Value = true;
                    playerEntity.IsJump.IsDirty = true;
                }
            }
        }

        EntityData.Velocity.Value = transform.rotation * dir.normalized * _moveSpeed;// + Vector3.up * EntityData.Velocity.Value.y;
        //if (isJump)
        //{
        //    EntityData.Velocity.Value = new Vector3(EntityData.Velocity.Value.x, _jumpPower, EntityData.Velocity.Value.z);
        //}
    }

    public void InventoryUpdate()
    {
        for(KeyCode key = KeyCode.Alpha1; key <= KeyCode.Alpha9; key++)
        {
            if (Input.GetKey(key))
            {
                if (EntityData is PlayerEntity playerEntity)
                {
                    playerEntity.Inventory.SelectSlotIndex.Value = key - KeyCode.Alpha1;
                }
            }
        }
    }

    public void EntityPacketUpdate()
    {
        Main.Instance.SendPacketMessage(MemoryPackSerializer.Serialize<IMPacket>(EntityData.UpdateDataTablePacket_Client())); ;
        EntityData.ClearDataTablePacket();
    }

    public void MouseUpdate()
    {
        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        Debug.DrawRay(Camera.main.transform.position, Camera.main.transform.forward * 100, Color.red);

        if (!(EntityData is PlayerEntity playerEntity))
        {
            return;
        }

        _hitGroundBox = null;
        _hitNormal = Vector3.zero;

        if (Physics.Raycast(ray, out var hit, 100))
        {
            if(hit.transform.TryGetComponent<Entity>(out var entity))
            {
                _hitGroundBox = entity.gameObject;
                _hitNormal = hit.normal;

                if (Input.GetMouseButtonDown(1))
                {
                    if (playerEntity.Inventory.TryGetCurrentItem(out Item item))
                    {
                        if (item.code == ItemCode.Block)
                        {
                            _groundModificationPacket.Position = Vector3Int.FloorToInt(_hitGroundBox.transform.position + _hitNormal);
                            _groundModificationPacket.IsCreate = true;
                            Main.Instance.SendPacketMessage(MemoryPackSerializer.Serialize<IMPacket>(_groundModificationPacket));
                            return;
                        }
                    }
                }
                else if (Input.GetMouseButtonDown(0))
                {
                    _groundModificationPacket.Position = Vector3Int.FloorToInt(_hitGroundBox.transform.position);
                    _groundModificationPacket.IsCreate = false;
                    Main.Instance.SendPacketMessage(MemoryPackSerializer.Serialize<IMPacket>(_groundModificationPacket));
                    return;
                }
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            playerEntity.HitDir.Value = transform.forward;
            playerEntity.HitDir.IsDirty = true;
        }
    }

    private void OnDrawGizmos()
    {
        if(_hitGroundBox != null)
        {
            Gizmos.DrawCube(_hitGroundBox.transform.position, Vector3.one);
            Gizmos.DrawLine(_hitGroundBox.transform.position, _hitGroundBox.transform.position + _hitNormal);
        }
    }

    public void MovementUpdate()
    {
        _jumpTimer += Time.deltaTime;
        _destPositionQueuingTimer += Time.deltaTime;
        if (EntityData.Position.IsDirty)
        {
            _lastPosition = transform.position;
            _moveTimer = 0.0f;
            _destinationInfo = (EntityData.Position.Value, _destPositionQueuingTimer);
            //_destPositionQueue.Enqueue((EntityData.Position.Value, _destPositionQueuingTimer));
            _destPositionQueuingTimer = 0.0f;
            EntityData.Position.IsDirty = false;
            _isStop = false;

            if (EntityData.IsTeleport.IsDirty && EntityData.IsTeleport.Value)
            {
                _moveTimer = _destinationInfo.time;
            }
            else if (Vector3.Distance(_lastPosition, _destinationInfo.position) < _moveSpeed * Time.deltaTime * 5.0f)
            {
                _moveTimer = _destinationInfo.time;
            }
        }

        _moveTimer += Time.deltaTime;

        if (!_isStop)
        {
            transform.position = Vector3.Lerp(_lastPosition, _destinationInfo.position, _moveTimer / _destinationInfo.time);
        }

        if (_moveTimer >= _destinationInfo.time)
        {
            _isStop = true;
        }
    }

    public void OnDestroy()
    {
    }
}
