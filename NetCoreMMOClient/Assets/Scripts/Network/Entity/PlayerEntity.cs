using NetCoreMMOServer.Network;
using NetCoreMMOServer.Packet;

public partial class PlayerEntity : EntityDataBase
{
    public SyncData<int> Hp = new(10);
    public SyncData<int> Power = new(1);

    public Inventory Inventory;
    public PlayerEntity() : base(EntityType.Player)
    {
        Hp = new(10);
        Power = new(1);

        _syncDatas.Add(Hp);
        _syncDatas.Add(Power);

        Inventory = new(9);
        foreach(var item in Inventory.Items)
        {
            _syncDatas.Add(item);
        }
        _syncDatas.Add(Inventory.SelectSlotIndex);

        Init(EntityInfo);
    }
}
