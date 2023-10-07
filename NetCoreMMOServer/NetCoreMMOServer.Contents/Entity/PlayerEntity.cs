using NetCoreMMOServer.Framework;
using NetCoreMMOServer.Network.Components.Contents;
using NetCoreMMOServer.Packet;
using NetCoreMMOServer.Physics;
using System.Numerics;

namespace NetCoreMMOServer.Contents.Entity
{
    public partial class PlayerEntity : NetEntity
    {
        public SyncData<int> Hp;
        public SyncData<int> Power;

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

            Components.Add(rigidBody);
            Components.Add(collider);
            //components.Add(inventory);

            Hp = new(10);
            Power = new(1);

            _syncDatas.Add(Hp);
            _syncDatas.Add(Power);

            Inventory = new(9);
            foreach (var item in Inventory.Items)
            {
                _syncDatas.Add(item);
            }
            Inventory.Items[0].Value = new Item() { code = ItemCode.Block, count = 10 }.buffer;
            _syncDatas.Add(Inventory.SelectSlotIndex);

            Init(new EntityInfo() { EntityID = EntityInfo.EntityID, EntityType = EntityType.Player });
        }

        public void GetDamage(int damage)
        {
            Hp.Value -= damage;
            if(Hp.Value <= 0)
            {
                // TODO :: Dead
                Transform.Position = Vector3.Zero;
                Hp.Value = 10;
            }
        }
    }
}
