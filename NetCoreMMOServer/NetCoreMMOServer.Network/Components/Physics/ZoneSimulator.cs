using NetCoreMMOServer.Network;
using System.Numerics;

namespace NetCoreMMOServer.Physics
{
    public class ZoneSimulator
    {
        private Zone _zone;

        private List<Collider> _currentZoneColliders;
        private List<RigidBody> _currentZoneRigidBodies;

        private List<Collider> _otherZoneColliders;

        public ZoneSimulator(Zone zone)
        {
            _zone = zone;
            _currentZoneColliders = new List<Collider>();
            _currentZoneRigidBodies = new List<RigidBody>();
            _otherZoneColliders = new List<Collider>();
        }

        public void ResetEntity()
        {
            _currentZoneColliders.Clear();
            _currentZoneRigidBodies.Clear();
            _otherZoneColliders.Clear();
        }

        public void AddEntity(EntityDataBase entity)
        {
            if(entity.CurrentZone.Value ==  _zone)
            {
                foreach (var component in entity.components)
                {
                    if (component is Collider collider)
                    {
                        _currentZoneColliders.Add(collider);
                    }
                    if (component is RigidBody rigidbody)
                    {
                        _currentZoneRigidBodies.Add(rigidbody);
                    }
                }
            }
            else
            {
                foreach (var component in entity.components)
                {
                    if (component is Collider collider)
                    {
                        _otherZoneColliders.Add(collider);
                    }
                }
            }
        }

        public void Update(float dt)
        {
            foreach (var rigidBody in _currentZoneRigidBodies)
            {
                if (rigidBody.IsStatic)
                {
                    continue;
                }
                // gravity
                float gravity = rigidBody.Velocity.Y;
                rigidBody.Velocity = rigidBody.Owner.Velocity.Value;
                //rigidBody.Velocity += new Vector3(0.0f, gravity + -9.81f * dt, 0.0f);
                if (rigidBody.Owner.Velocity.Value.Y == 0.0f)
                {
                    //rigidBody.Velocity += new Vector3(0.0f, gravity + -9.81f * dt, 0.0f);
                    rigidBody.Velocity += new Vector3(0.0f, gravity, 0.0f) + PhysicsOption.Gravity * dt;
                }
                else
                {
                    rigidBody.Owner.Velocity.Value = new Vector3(rigidBody.Owner.Velocity.Value.X, 0.0f, rigidBody.Owner.Velocity.Value.Z);
                }
            }

            //for (int i = 0; i < 10; i++)
            //{
            //    Step(dt * 0.1f);
            //}
        }

        public void Step(float time)
        {
            foreach (var rigidBody in _currentZoneRigidBodies)
            {
                if (rigidBody.IsStatic)
                {
                    continue;
                }
                // gravity
                rigidBody.Owner.Position.Value += rigidBody.Velocity * time;
            }

            for (int i = 0; i < _currentZoneColliders.Count - 1; i++)
            {
                for (int j = i + 1; j < _currentZoneColliders.Count; j++)
                {
                    if (!_currentZoneColliders[i].CheckCollision(_currentZoneColliders[j], out Vector3 normal, out float depth))
                    {
                        continue;
                    }

                    if (_currentZoneColliders[i].IsTrigger)
                    {
                        _currentZoneColliders[i].OnTrigger(_currentZoneColliders[j]);
                    }
                    if (_currentZoneColliders[j].IsTrigger)
                    {
                        _currentZoneColliders[j].OnTrigger(_currentZoneColliders[i]);
                    }
                    if (_currentZoneColliders[i].IsTrigger || _currentZoneColliders[j].IsTrigger)
                    {
                        continue;
                    }

                    _currentZoneColliders[i].OnCollider(_currentZoneColliders[j]);
                    _currentZoneColliders[j].OnCollider(_currentZoneColliders[i]);

                    Solve(_currentZoneColliders[i].AttachedRigidbody, _currentZoneColliders[j].AttachedRigidbody, normal, depth);
                }
            }

            foreach(var currentCollider in  _currentZoneColliders)
            {
                foreach(var otherCollider in _otherZoneColliders)
                {
                    if (!currentCollider.CheckCollision(otherCollider, out Vector3 normal, out float depth))
                    {
                        continue;
                    }

                    if (currentCollider.IsTrigger)
                    {
                        currentCollider.OnTrigger(otherCollider);
                    }
                    if (otherCollider.IsTrigger)
                    {
                        otherCollider.OnTrigger(currentCollider);
                    }
                    if (currentCollider.IsTrigger || otherCollider.IsTrigger)
                    {
                        continue;
                    }

                    currentCollider.OnCollider(otherCollider);
                    otherCollider.OnCollider(currentCollider);

                    Solve(currentCollider.AttachedRigidbody, otherCollider.AttachedRigidbody, normal, depth);
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
            else if (bodyBStatic)
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
                currentBody!.Owner.Position.Value += -normal * depth;
                currentBody!.Velocity -= impulse * currentBody.InvMass;
            }
            else
            {
                float depthAmount = depth * 0.5f;
                currentBody!.Owner.Position.Value += -normal * depthAmount;
                currentBody!.Velocity -= impulse * currentBody.InvMass;
            }
        }
    }
}
