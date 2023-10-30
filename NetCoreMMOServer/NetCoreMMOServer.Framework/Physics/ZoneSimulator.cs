using NetCoreMMOServer.Framework;
using System.Numerics;

namespace NetCoreMMOServer.Physics
{
    public class ZoneSimulator : Simulator
    {
        private Zone _zone;

        private List<ColliderComponent> _otherZoneColliderComponents;
        private List<RigidBodyComponent> _rigidBodyComponents;
        private List<ColliderComponent> _colliderComponents;

        public ZoneSimulator(Zone zone)
        {
            _zone = zone;
            _otherZoneColliderComponents = new List<ColliderComponent>();
            _rigidBodyComponents = new List<RigidBodyComponent>();
            _colliderComponents = new List<ColliderComponent>();
        }

        public new void ResetEntity()
        {
            base.ResetEntity();
            _otherZoneColliderComponents.Clear();
            _rigidBodyComponents.Clear();
        }

        public void AddEntity(NetEntity entity)
        {
            if (entity.CurrentZone.Value == _zone)
            {
                foreach (var component in entity.Components)
                {
                    if (component is ColliderComponent collider)
                    {
                        _colliders.Add(collider.Collider);
                        _colliderComponents.Add(collider);
                    }
                    if (component is RigidBodyComponent rigidbody)
                    {
                        _rigidBodies.Add(rigidbody.RigidBody);
                        _rigidBodyComponents.Add(rigidbody);
                    }
                }
            }
            else
            {
                foreach (var component in entity.Components)
                {
                    if (component is ColliderComponent collider)
                    {
                        _otherZoneColliderComponents.Add(collider);
                    }
                }
            }
        }

        public void RemoveEntity(NetEntity entity)
        {
            if (entity.CurrentZone.Value == _zone)
            {
                foreach (var component in entity.Components)
                {
                    if (component is ColliderComponent collider)
                    {
                        _colliders.Remove(collider.Collider);
                    }
                    if (component is RigidBodyComponent rigidbody)
                    {
                        _rigidBodies.Remove(rigidbody.RigidBody);
                        _rigidBodyComponents.Remove(rigidbody);
                    }
                }
            }
            else
            {
                foreach (var component in entity.Components)
                {
                    if (component is ColliderComponent collider)
                    {
                        _otherZoneColliderComponents.Remove(collider);
                    }
                }
            }
        }

        public override void Update(float dt)
        {
            foreach (var rigidBody in _rigidBodyComponents)
            {
                if (rigidBody.RigidBody.IsStatic)
                {
                    continue;
                }
                // gravity
                rigidBody.RigidBody.PrevVelcotiy = rigidBody.RigidBody.Velocity;
                //float currentGravity = rigidBody.RigidBody.Velocity.Y;
                Vector3 velocity = rigidBody.RigidBody.Velocity + rigidBody.RigidBody.NextVelocity;
                velocity += PhysicsOption.Gravity * dt;
                rigidBody.RigidBody.Velocity = velocity;
                rigidBody.RigidBody.NextVelocity = new Vector3(0.0f, 0.0f, 0.0f);
            }
        }

        public override void Step(float time)
        {
            base.Step(time);

            foreach (var currentCollider in _colliders)
            {
                foreach (var otherCollider in _otherZoneColliderComponents)
                {
                    if (!currentCollider.CheckCollision(otherCollider.Collider, out Vector3 normal, out float depth))
                    {
                        continue;
                    }

                    if (currentCollider.IsTrigger)
                    {
                        currentCollider.OnTrigger(otherCollider.Collider);
                    }
                    if (otherCollider.Collider.IsTrigger)
                    {
                        otherCollider.Collider.OnTrigger(currentCollider);
                    }
                    if (currentCollider.IsTrigger || otherCollider.Collider.IsTrigger)
                    {
                        continue;
                    }

                    currentCollider.OnCollider(otherCollider.Collider);
                    otherCollider.Collider.OnCollider(currentCollider);

                    Solve(currentCollider.AttachedRigidbody, otherCollider.Collider.AttachedRigidbody, normal, depth);
                }
            }
        }

        public bool CheckCollision(Collider collider, out Entity? entity, int layerMask)
        {
            entity = null;

            foreach (var currentCollider in _colliderComponents)
            {
                if ((currentCollider.Owner.Layer & layerMask) == 0)
                {
                    continue;
                }
                
                if (collider.CheckCollision(currentCollider.Collider, out Vector3 _, out float _))
                {
                    entity = currentCollider.Owner;
                    return true;
                }
            }

            foreach (var otherCollider in _otherZoneColliderComponents)
            {
                if ((otherCollider.Owner.Layer & layerMask) == 0)
                {
                    continue;
                }

                if (collider.CheckCollision(otherCollider.Collider, out Vector3 _, out float _))
                {
                    entity = otherCollider.Owner;
                    return true;
                }
            }

            return false;
        }

        // MultiThreading 오류 시 참고 해볼것
        private void CurrentSolve(RigidBody? currentBody, RigidBody? otherBody, in Vector3 normal, in float depth)
        {
            bool bodyAStatic = currentBody?.IsStatic ?? true;
            bool bodyBStatic = otherBody?.IsStatic ?? true;

            if (bodyAStatic && bodyBStatic)
            {
                return;
            }

            Vector3 relativeVelocity = otherBody?.Velocity ?? Vector3.Zero - currentBody?.Velocity ?? Vector3.Zero;
            Vector3 impulse = Vector3.Zero;
            if (Vector3.Dot(relativeVelocity, normal) < 0)
            {
                float j = -1f * Vector3.Dot(relativeVelocity, normal);
                j /= (currentBody?.InvMass ?? 0f) + (otherBody?.InvMass ?? 0f);
                impulse = j * normal;
            }

            if (bodyAStatic)
            {
                //otherBody!.Owner.Position.Value += normal * depth;
                //otherBody!.Velocity += impulse * otherBody.InvMass;
            }
            else if (bodyBStatic)
            {
                currentBody!.Transform!.Position += -normal * depth;
                currentBody!.Velocity -= impulse * currentBody.InvMass;
            }
            else
            {
                float depthAmount = depth * 0.5f;
                currentBody!.Transform!.Position += -normal * depthAmount;
                currentBody!.Velocity -= impulse * currentBody.InvMass;
            }
        }
    }
}
