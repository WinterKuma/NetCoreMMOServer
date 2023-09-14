using NetCoreMMOServer.Network;
using NetCoreMMOServer.Packet;

public partial class PlayerEntity : EntityDataBase
{
    public Inventory Inventory;
    public PlayerEntity() : base(EntityType.Player)
    {
        Inventory = new(9);

        foreach(var item in Inventory.Items)
        {
            _syncDatas.Add(item);
        }

        Init(EntityInfo);
    }
}
