using System.Numerics;

namespace NetCoreMMOServer.Physics
{
    public abstract class Collider
    {
        private RigidBody? _attachedRigidbody = null;
        private bool _isTrigger;
        private Transform _transform;

        public Collider(Transform transform, RigidBody? attachedRigidbody = null, bool isTrigger = false)
        {
            _attachedRigidbody = attachedRigidbody;
            _isTrigger = isTrigger;
            _transform = transform;
        }

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

        public Transform Transform
        {
            get { return _transform; }
            set { _transform = value; }
        }

        public abstract bool CheckCollision(Collider other, out Vector3 normal, out float depth);
        public virtual void OnCollider(Collider other) { }
        public virtual void OnTrigger(Collider other) { }
    }
}
