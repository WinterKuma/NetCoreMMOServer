using NetCoreMMOServer.Framework;
using NetCoreMMOServer.Network.Components.Contents;
using NetCoreMMOServer.Packet;
using NetCoreMMOServer.Physics;
using System.Numerics;

namespace NetCoreMMOServer.Contents.Entity
{
    public abstract partial class PlayerEntity : NetEntity
    {
        // Server Side
        public SyncData<int> Hp;
        public SyncData<int> Power;

        // Client Side
        public SyncData<bool> IsJump;
        public SyncData<Vector3> HitDir;

        // Component
        public Inventory Inventory;

        private RigidBodyComponent _rigidBodyComponent;

        public PlayerEntity()
        {
            _layer = 1;

            _rigidBodyComponent = new();
            _rigidBodyComponent.RigidBody.Transform = Transform;

            SphereCollider sphereCollider = new SphereCollider(Transform, _rigidBodyComponent.RigidBody);
            sphereCollider.Offset = Vector3.Zero;
            sphereCollider.Radius = 0.5f;

            ColliderComponent collider = new(sphereCollider);

            _rigidBodyComponent.SetEntityDataBase(this);
            collider.SetEntityDataBase(this);

            Components.Add(_rigidBodyComponent);
            Components.Add(collider);
            //components.Add(inventory);

            Hp = new(10);
            Power = new(1);

            _serverSideSyncDatas.Add(Hp);
            _serverSideSyncDatas.Add(Power);

            Inventory = new(9);
            foreach (var item in Inventory.Items)
            {
                _serverSideSyncDatas.Add(item);
            }
            Inventory.Items[0].Value = new Item() { code = ItemCode.Block, count = 10 }.buffer;
            _clientSideSyncDatas.Add(Inventory.SelectSlotIndex);

            IsJump = new(false);
            _clientSideSyncDatas.Add(IsJump);

            HitDir = new(new Vector3());
            _clientSideSyncDatas.Add(HitDir);

            //_attackCollider = new SphereCollider(Transform);
            //_attackCollider.Offset = Vector3.Zero;
            //_attackCollider.Radius = 0.5f;

            Init(new EntityInfo() { EntityID = EntityInfo.EntityID, EntityType = EntityType.Player });
        }

        public RigidBodyComponent RigidBodyComponent => _rigidBodyComponent;

        public override void Update(float dt)
        {
        }

        [ClientRPC]
        public abstract void Attack();

        [ClientRPC]
        public abstract void Test(Vector3 value);

        [ServerRPC]
        public abstract void GetDamage(int damage);

        [ServerRPC]
        public abstract void Test3(int val1, float val2);
    }
}