using NetCoreMMOServer.Framework;
using NetCoreMMOServer.Network.Components.Contents;
using NetCoreMMOServer.Packet;
using NetCoreMMOServer.Physics;
using System.Numerics;

namespace NetCoreMMOServer.Contents.Entity
{
    public partial class PlayerEntity : EntityDataBase
    {
        public SyncData<int> Power = new(1);
        public SyncData<int> Hp = new(10);
    }

    public partial class PlayerEntity : EntityDataBase
    {
        public Inventory Inventory;

        public PlayerEntity()
        {
            //_syncDatas.Add(power);
            //_syncDatas.Add(hp);

            RigidBodyComponent rigidBody = new();
            rigidBody.RigidBody.Transform = Transform;

            SphereCollider sphereCollider = new SphereCollider(Transform, rigidBody.RigidBody);
            sphereCollider.Offset = Vector3.Zero;
            sphereCollider.Radius = 0.5f;

            ColliderComponent collider = new(sphereCollider);

            rigidBody.SetEntityDataBase(this);
            collider.SetEntityDataBase(this);

            Inventory = new(9);

            components.Add(rigidBody);
            components.Add(collider);
            //components.Add(inventory);

            foreach (var item in Inventory.Items)
            {
                _syncDatas.Add(item);
            }
            Inventory.Items[0].Value = new Item() { code = ItemCode.Block, count = 10 }.buffer;

            Init(new EntityInfo() { EntityID = EntityInfo.EntityID, EntityType = EntityType.Player });
        }
    }
}
