using NetCoreMMOServer.Framework;
using NetCoreMMOServer.Packet;
using NetCoreMMOServer.Physics;
using System.Numerics;

namespace NetCoreMMOServer.Contents.Entity
{
    public partial class BlockEntity : NetEntity
    {
        public BlockEntity()
        {
            RigidBodyComponent rigidBody = new(1f, true);
            rigidBody.RigidBody.Transform = Transform;

            CubeCollider cubeCollider = new(Transform, rigidBody.RigidBody);
            cubeCollider.Offset = Vector3.Zero;
            cubeCollider.Size = Vector3.One;
            ColliderComponent collider = new(cubeCollider);

            rigidBody.SetEntityDataBase(this);
            collider.SetEntityDataBase(this);

            Components.Add(rigidBody);
            Components.Add(collider);

            Init(new EntityInfo() { EntityID = EntityInfo.EntityID, EntityType = EntityType.Block });
        }
    }
}
