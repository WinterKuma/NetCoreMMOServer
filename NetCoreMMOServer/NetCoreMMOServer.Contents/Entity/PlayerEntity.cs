using NetCoreMMOServer.Framework;
using NetCoreMMOServer.Network.Components.Contents;
using NetCoreMMOServer.Packet;
using NetCoreMMOServer.Physics;
using System.Numerics;

namespace NetCoreMMOServer.Contents.Entity
{
    public partial class PlayerEntity : NetEntity
    {
        // Server Side
        public SyncData<int> Hp;
        public SyncData<int> Power;

        // Client Side
        public SyncData<bool> IsJump;
        public SyncData<Vector3> HitDir;

        // Component
        public Inventory Inventory;

        // Etc
        //public List<ColliderComponent> hitCollider;
        private SphereCollider attackCollider;

        public PlayerEntity()
        {
            _layer = 1;

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

            attackCollider = new SphereCollider(Transform);
            attackCollider.Offset = Vector3.Zero;
            attackCollider.Radius = 0.5f;

            Init(new EntityInfo() { EntityID = EntityInfo.EntityID, EntityType = EntityType.Player });
        }

        public override void Update(float dt)
        {
            if (IsJump.IsDirty && IsJump.Value)
            {
                Velocity.Value += new Vector3(0.0f, 6.0f, 0.0f);
            }
            if (HitDir.IsDirty)
            {
                attackCollider.Offset = HitDir.Value * 1.5f;
                if (CurrentZone.Value!.PhysicsSimulator.CheckCollision(attackCollider, out Framework.Entity? entity, Layer))
                {
                    if (entity is PlayerEntity player)
                    {
                        player.GetDamage(Power.Value);
                    }
                }
                 
            }
        }

        public void GetDamage(int damage)
        {
            Hp.Value -= damage;
            if(Hp.Value <= 0)
            {
                // TODO :: Dead
                Teleport(Vector3.Zero);
                Hp.Value = 10;
            }
        }
    }
}
