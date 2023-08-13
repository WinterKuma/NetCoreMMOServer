using Codice.CM.WorkspaceServer.Tree.GameUI.Checkin.Updater;
using MemoryPack;
using NetCoreMMOClient.Utility;
using NetCoreMMOServer.Network;
using NetCoreMMOServer.Packet;
using Sirenix.OdinInspector;
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
    private MeshRenderer renderer;

    [field: SerializeField]
    private Material mineMaterial;

    // Start is called before the first frame update
    void Start()
    {
        if (IsMine)
        {
            Camera.main.transform.parent = transform;
            renderer.material = mineMaterial;
        }
        else
        {
            _lastPosition = transform.position;
            _destinationInfo = (transform.position, 1.0f);
        }
    }

    // Update is called once per frame
    void Update()
    {
        _jumpTimer += Time.deltaTime;
        _destPositionQueuingTimer += Time.deltaTime;
        if (EntityData.Position.IsDirty)
        {
            _lastPosition = transform.position;
            _moveTimer = 0.0f;
            _destinationInfo = (EntityData.Position.Value, _destPositionQueuingTimer);
            _destPositionQueue.Enqueue((EntityData.Position.Value, _destPositionQueuingTimer));
            _destPositionQueuingTimer = 0.0f;
            EntityData.Position.IsDirty = false;
            _isStop = false;

            if(Vector3.Distance(_lastPosition, _destinationInfo.position) < _moveSpeed * Time.deltaTime * 5.0f)
            {
                _moveTimer = _destinationInfo.time;
            }
        }

        _moveTimer += Time.deltaTime;

        if (!_isStop)
        {
            transform.position = Vector3.Lerp(_lastPosition, _destinationInfo.position, _moveTimer / _destinationInfo.time);
        }

        if(_moveTimer >= _destinationInfo.time)
        {
            _isStop = true;
        }

        //float d = Vector3.Distance(transform.position, _destinationInfo.position);
        //if (d < _moveSpeed * Time.deltaTime)
        //{
        //    transform.position = Vector3.MoveTowards(transform.position, _destinationInfo.position, _moveSpeed * Time.deltaTime);
        //}
        //else if(d < _moveSpeed * Time.deltaTime * 3.0f)
        //{
        //    transform.position = Vector3.MoveTowards(transform.position, _destinationInfo.position, _moveSpeed * Time.deltaTime * 3.0f);
        //}
        //else
        //{
        //    transform.position = _destinationInfo.position;
        //}
        //if (_destPositionQueue.Count > 2)
        //{
        //    _lastPosition = _destinationInfo.position;
        //    _isStop = false;
        //    _moveTimer = 0.0f;
        //    _destinationInfo = _destPositionQueue.Dequeue();
        //}

        //if (!_isStop)
        //{
        //    _moveTimer += Time.deltaTime;
        //    if (_moveTimer < _destinationInfo.time)
        //    {
        //        transform.position = Vector3.MoveTowards(transform.position, Vector3.Lerp(_lastPosition, _destinationInfo.position, _moveTimer / _destinationInfo.time), _moveSpeed * Time.deltaTime);
        //    }
        //    else
        //    {
        //        _lastPosition = _destinationInfo.position;
        //        _moveTimer -= _destinationInfo.time;
        //        if (_destPositionQueue.TryDequeue(out _destinationInfo))
        //        {
        //            transform.position = Vector3.MoveTowards(transform.position, Vector3.Lerp(_lastPosition, _destinationInfo.position, _moveTimer / _destinationInfo.time), _moveSpeed * Time.deltaTime);
        //        }
        //        else
        //        {
        //            transform.position = _lastPosition;
        //        }
        //    }
        //}

        if (IsMine)
        {
            bool isMove = false;
            bool isJump = false;
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
                dir += Vector3.forward;
            }
            if (Input.GetKey(KeyCode.DownArrow))
            {
                isMove = true;
                dir += Vector3.back;
            }
            if (Input.GetKey(KeyCode.Space))
            {
                if (_jumpTimer >= _jumpDelay)
                {
                    isJump = true;
                    _jumpTimer = 0.0f;
                }
            }
            if (isMove)
            {
                if(_isStop)
                {
                    //transform.position += dir.normalized * _moveSpeed * Time.deltaTime;
                }
                //transform.position = transform.position + dir.normalized * _moveSpeed * Time.fixedDeltaTime;
                //EntityData.Velocity.Value = dir.normalized * _moveSpeed;
                //EntityData.Position.Value = transform.position;
                //MoveEntity(dir);
            }
            EntityData.Velocity.Value = dir.normalized * _moveSpeed + Vector3.up * EntityData.Velocity.Value.y;
            if (isJump)
            {
                EntityData.Velocity.Value = new Vector3(EntityData.Velocity.Value.x, _jumpPower, EntityData.Velocity.Value.z);
            }

            Main.Instance.SendPacketMessage(MemoryPackSerializer.Serialize<IMPacket>(EntityData.UpdateDataTablePacket()));
            EntityData.ClearDataTablePacket();
        }
    }

    public void OnDestroy()
    {
    }
}
