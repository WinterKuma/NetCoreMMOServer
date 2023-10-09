using NetCoreMMOServer.Network;
using NetCoreMMOServer.Packet;
using UnityEngine;

public partial class PlayerEntity : EntityDataBase
{
    public SyncData<int> Hp = new(10);
    public SyncData<int> Power = new(1);

    public SyncData<bool> IsJump;
    public SyncData<Vector3> HitDir;

    public Inventory Inventory;
    public PlayerEntity() : base(EntityType.Player)
    {
        Hp = new(10);
        Power = new(1);

        _serverSideSyncDatas.Add(Hp);
        _serverSideSyncDatas.Add(Power);

        Inventory = new(9);
        foreach(var item in Inventory.Items)
        {
            _serverSideSyncDatas.Add(item);
        }
        _clientSideSyncDatas.Add(Inventory.SelectSlotIndex);

        IsJump = new(false);
        _clientSideSyncDatas.Add(IsJump);

        HitDir = new(new Vector3());
        _clientSideSyncDatas.Add(HitDir);

        Init(EntityInfo);
    }
}
