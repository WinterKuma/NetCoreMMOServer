using NetCoreMMOServer.Network.Component;
using System.Numerics;

namespace NetCoreMMOServer.Physics
{
    public abstract class Collider : Component
    {
        private RigidBody? _attachedRigidbody = null;
        private bool _isTrigger;

        public bool IsTrigger
        {
            get { return _isTrigger; }
            set { _isTrigger = value; }
        }

        public RigidBody? AttachedRigidbody
        {
            get { return _attachedRigidbody; }
            set { _attachedRigidbody = value; }
        }

        public abstract bool CheckCollision(Collider other, out Vector3 normal, out float depth);
        public virtual void OnCollider(Collider other) { }
        public virtual void OnTrigger(Collider other) { }
    }
}
