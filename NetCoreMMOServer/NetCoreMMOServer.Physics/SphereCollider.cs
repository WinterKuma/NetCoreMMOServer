using NetCoreMMOServer.Network.Component.Physics;
using System.Numerics;

namespace NetCoreMMOServer.Physics
{
    public class SphereCollider : Collider
    {
        private Vector3 _offset;
        private float _radius;

        public SphereCollider(Transform transform, RigidBody? attachedRigidbody = null, bool isTrigger = false) : base(transform, attachedRigidbody, isTrigger)
        {
        }

        public Vector3 Center => Transform.Position + _offset;
        public Vector3 Offset
        {
            get { return _offset; }
            set { _offset = value; }
        }

        public float Radius
        {
            get { return _radius; }
            set { _radius = value; }
        }

        public float Diameter => _radius * 2;

        public override bool CheckCollision(Collider other, out Vector3 normal, out float depth)
        {
            switch (other)
            {
                case CubeCollider cube:
                    return this.IsCollideWithCube(cube, out normal, out depth);

                case SphereCollider sphere:
                    return this.IsCollideWithSphere(sphere, out normal, out depth);

                default:
                    normal = Vector3.Zero;
                    depth = 0;
                    return false;
            }
        }
    }
}
