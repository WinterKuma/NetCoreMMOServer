using NetCoreMMOServer.Network;
using System.Numerics;

namespace NetCoreMMOServer.Physics
{
    public class Simulator
    {
        protected List<Collider> _colliders;
        protected List<RigidBody> _rigidBodies;

        private int _stepCount = 10;
        private float _invStepCount = 0.1f;

        public Simulator()
        {
            _colliders = new List<Collider>();
            _rigidBodies = new List<RigidBody>();
        }

        public int StepCount
        {
            get { return _stepCount; }
            set 
            { 
                _stepCount = value;
                _invStepCount = 1.0f / value;
            }
        }

        public float invStepCount => _invStepCount;

        public void ResetEntity()
        {
            _colliders.Clear();
            _rigidBodies.Clear();
        }

        public void AddCollider(Collider collider)
        {
            _colliders.Add(collider);
        }

        public bool RemoveCollider(Collider collider)
        {
            return _colliders.Remove(collider);
        }

        public void AddRigidBody(RigidBody rigidBody)
        {
            _rigidBodies.Add(rigidBody);
        }

        public bool RemoveRigidBody(RigidBody rigidBody)
        {
            return _rigidBodies.Remove(rigidBody);
        }

        public virtual void Update(float dt)
        {
            foreach (var rigidBody in _rigidBodies)
            {
                if (rigidBody.IsStatic)
                {
                    continue;
                }
                // gravity
                float gravity = rigidBody.Velocity.Y;
                rigidBody.PrevVelcotiy = rigidBody.Velocity;
                rigidBody.Velocity = rigidBody.NextVelocity;
                if(rigidBody.Velocity.Y == 0.0f)
                {
                    rigidBody.Velocity += new Vector3(0.0f, gravity, 0.0f) + PhysicsOption.Gravity * dt;
                }
                else
                {
                    rigidBody.Velocity = new Vector3(rigidBody.Velocity.X, 0.0f, rigidBody.Velocity.Z);
                }
            }

            for(int i = 0; i < _stepCount; i++)
            {
                Step(dt * _invStepCount);
            }
        }

        public virtual void Step(float time)
        {
            foreach (var rigidBody in _rigidBodies)
            {
                if(rigidBody.IsStatic || rigidBody.Transform == null)
                {
                    continue;
                }
                // gravity
                rigidBody.Transform.Position += rigidBody.Velocity * time;
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

        protected void Solve(RigidBody? bodyA, RigidBody? bodyB, in Vector3 normal, in float depth)
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
                bodyB!.Transform!.Position += normal * depth;
                bodyB!.Velocity += impulse * bodyB.InvMass;
            }
            else if(bodyBStatic)
            {
                bodyA!.Transform!.Position += -normal * depth;
                bodyA!.Velocity -= impulse * bodyA.InvMass;
            }
            else
            {
                float depthAmount = depth * 0.5f;
                bodyA!.Transform!.Position += -normal * depthAmount;
                bodyA!.Velocity -= impulse * bodyA.InvMass;

                bodyB!.Transform!.Position += normal * depthAmount;
                bodyB!.Velocity += impulse * bodyB.InvMass;
            }
        }
    }
}
