using NetCoreMMOServer.Physics;
using System.Numerics;

namespace NetCoreMMOServer.Contents.Entity.Master
{
    public class Master_PlayerEntity : Master_PlayerEntity_Base
    {
        private SphereCollider _attackCollider;
        public Master_PlayerEntity()
        {
            _attackCollider = new SphereCollider(Transform);
            _attackCollider.Offset = Vector3.Zero;
            _attackCollider.Radius = 0.5f;
        }

        public override void Attack()
        {
            Console.WriteLine($"{EntityInfo.EntityID} : Attack()");

            //this.Transform.Rotation * ;
            //_attackCollider.Offset = HitDir.Value * 1.5f;
            //if (CurrentZone.Value!.PhysicsSimulator.CheckCollision(_attackCollider, out Framework.Entity? entity, Layer))
            //{
            //    if (entity is PlayerEntity player)
            //    {
            //        Vector3 dir = player.Transform.Position - Transform.Position;
            //        dir.Y = 0.0f;
            //        dir = dir / dir.Length();
            //        player.RigidBodyComponent.RigidBody.NextVelocity += dir * 8.0f + Vector3.UnitY * 3.0f;
            //        player.GetDamage(Power.Value);
            //    }
            //}
        }

        public override void Test(Vector3 value)
        {
            Console.WriteLine($"{EntityInfo.EntityID} : Test({value})");
        }

        public override void Test2(int a, int b, char c, Vector3 v)
        {
            Console.WriteLine($"{EntityInfo.EntityID} : Test({a}, {b}, {c}, {v})");
        }

        public override void Update(float dt)
        {
            RigidBodyComponent.RigidBody.Velocity = new Vector3(Velocity.Value.X, RigidBodyComponent.RigidBody.Velocity.Y, Velocity.Value.Z);
            if (IsJump.IsDirty && IsJump.Value)
            {
                RigidBodyComponent.RigidBody.Velocity += new Vector3(0.0f, 6.0f, 0.0f);
            }
        }
    }
}
