using NetCoreMMOServer.Network;
using System.Numerics;

namespace NetCoreMMOServer.Physics
{
    public class Simulator
    {
        private List<Collider> colliders;
        private List<RigidBody> rigidBodies;

        public Simulator()
        {
            colliders = new List<Collider>();
            rigidBodies = new List<RigidBody>();
        }

        public void ResetEntity()
        {
            colliders.Clear();
            rigidBodies.Clear();
        }

        public void AddEntity(EntityDataBase entity)
        {
            foreach(var component in entity.components)
            {
                if (component is Collider collider)
                {
                    colliders.Add(collider);
                }
                if (component is RigidBody rigidbody)
                {
                    rigidBodies.Add(rigidbody);
                }
            }
        }

        public void Update(float dt)
        {
            foreach (var rigidBody in rigidBodies)
            {
                if (rigidBody.IsStatic)
                {
                    continue;
                }
                // gravity
                rigidBody.Velocity = rigidBody.Owner.Velocity.Value;
            }

            for(int i = 0; i < 10; i++)
            {
                Step(dt * 0.1f);
            }
        }

        private void Step(float time)
        {
            foreach (var rigidBody in rigidBodies)
            {
                if(rigidBody.IsStatic)
                {
                    continue;
                }
                // gravity
                rigidBody.Owner.Position.Value += rigidBody.Velocity * time;
            }

            for (int i = 0; i < colliders.Count - 1; i++)
            {
                for(int j = i + 1; j < colliders.Count; j++)
                {
                    if (!colliders[i].CheckCollision(colliders[j], out Vector3 normal, out float depth))
                    {
                        continue;
                    }

                    if (colliders[i].IsTrigger)
                    {
                        colliders[i].OnTrigger(colliders[j]);
                    }
                    if (colliders[j].IsTrigger)
                    {
                        colliders[j].OnTrigger(colliders[i]);
                    }
                    if (colliders[i].IsTrigger || colliders[j].IsTrigger)
                    {
                        continue;
                    }

                    colliders[i].OnCollider(colliders[j]);
                    colliders[j].OnCollider(colliders[i]);

                    Solve(colliders[i].AttachedRigidbody, colliders[j].AttachedRigidbody, normal, depth);
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

            float depthAmout = depth;

            if(bodyAStatic)
            {
                bodyB!.Owner.Position.Value += normal * depth;
            }
            else if(bodyBStatic)
            {
                bodyA!.Owner.Position.Value += normal * depth;
            }
            else
            {
                float depthAmount = depth * 0.5f;
                bodyA!.Owner.Position.Value += -normal * depth;
                bodyB!.Owner.Position.Value += normal * depth;
            }
        }
    }
}
