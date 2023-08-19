using NetCoreMMOServer.Network;
using System.Numerics;

namespace NetCoreMMOServer.Physics
{
    public class Simulator
    {
        private List<Collider> _colliders;
        private List<RigidBody> _rigidBodies;

        public Simulator()
        {
            _colliders = new List<Collider>();
            _rigidBodies = new List<RigidBody>();
        }

        public void ResetEntity()
        {
            _colliders.Clear();
            _rigidBodies.Clear();
        }

        public void AddEntity(EntityDataBase entity)
        {
            foreach(var component in entity.components)
            {
                if (component is Collider collider)
                {
                    _colliders.Add(collider);
                }
                if (component is RigidBody rigidbody)
                {
                    _rigidBodies.Add(rigidbody);
                }
            }
        }

        public void Update(float dt)
        {
            foreach (var rigidBody in _rigidBodies)
            {
                if (rigidBody.IsStatic)
                {
                    continue;
                }
                // gravity
                float gravity = rigidBody.Velocity.Y;
                rigidBody.Velocity = rigidBody.Owner.Velocity.Value;
                //rigidBody.Velocity += new Vector3(0.0f, gravity + -9.81f * dt, 0.0f);
                if(rigidBody.Owner.Velocity.Value.Y == 0.0f)
                {
                    //rigidBody.Velocity += new Vector3(0.0f, gravity + -9.81f * dt, 0.0f);
                    rigidBody.Velocity += new Vector3(0.0f, gravity, 0.0f) + PhysicsOption.Gravity * dt;
                }
                else
                {
                    rigidBody.Owner.Velocity.Value = new Vector3(rigidBody.Owner.Velocity.Value.X, 0.0f, rigidBody.Owner.Velocity.Value.Z);
                }
            }

            for(int i = 0; i < 10; i++)
            {
                Step(dt * 0.1f);
            }
        }

        private void Step(float time)
        {
            foreach (var rigidBody in _rigidBodies)
            {
                if(rigidBody.IsStatic)
                {
                    continue;
                }
                // gravity
                rigidBody.Owner.Position.Value += rigidBody.Velocity * time;
            }

            for (int i = 0; i < _colliders.Count - 1; i++)
            {
                for(int j = i + 1; j < _colliders.Count; j++)
                {
                    if (!_colliders[i].CheckCollision(_colliders[j], out Vector3 normal, out float depth))
                    {
                        continue;
                    }

                    if (_colliders[i].IsTrigger)
                    {
                        _colliders[i].OnTrigger(_colliders[j]);
                    }
                    if (_colliders[j].IsTrigger)
                    {
                        _colliders[j].OnTrigger(_colliders[i]);
                    }
                    if (_colliders[i].IsTrigger || _colliders[j].IsTrigger)
                    {
                        continue;
                    }

                    _colliders[i].OnCollider(_colliders[j]);
                    _colliders[j].OnCollider(_colliders[i]);

                    Solve(_colliders[i].AttachedRigidbody, _colliders[j].AttachedRigidbody, normal, depth);
                }
            }
        }

        private void Solve(RigidBody? bodyA, RigidBody? bodyB, in Vector3 normal, in float depth)
        {
            bool bodyAStatic = bodyA?.IsStatic ?? true;
            bool bodyBStatic = bodyB?.IsStatic ?? true;

            if (bodyAStatic && bodyBStatic)
            {
                return;
            }

            Vector3 relativeVelocity = bodyB?.Velocity ?? Vector3.Zero - bodyA?.Velocity ?? Vector3.Zero;
            Vector3 impulse = Vector3.Zero;
            if (Vector3.Dot(relativeVelocity, normal) < 0)
            {
                float j = -1f * Vector3.Dot(relativeVelocity, normal);
                j /= (bodyA?.InvMass ?? 0f) + (bodyB?.InvMass ?? 0f);
                impulse = j * normal;
            }


            if (bodyAStatic)
            {
                bodyB!.Owner.Position.Value += normal * depth;
                bodyB!.Velocity += impulse * bodyB.InvMass;
            }
            else if(bodyBStatic)
            {
                bodyA!.Owner.Position.Value += -normal * depth;
                bodyA!.Velocity -= impulse * bodyA.InvMass;
            }
            else
            {
                float depthAmount = depth * 0.5f;
                bodyA!.Owner.Position.Value += -normal * depthAmount;
                bodyA!.Velocity -= impulse * bodyA.InvMass;

                bodyB!.Owner.Position.Value += normal * depthAmount;
                bodyB!.Velocity += impulse * bodyB.InvMass;
            }
        }
    }
}
