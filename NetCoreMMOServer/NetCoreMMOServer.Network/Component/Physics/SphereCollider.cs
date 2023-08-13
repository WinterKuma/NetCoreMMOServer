using NetCoreMMOServer.Network.Component.Physics;
using System.Numerics;

namespace NetCoreMMOServer.Physics
{
    public class SphereCollider : Collider
    {
        private Vector3 _offset;
        private float _radius;

        public Vector3 Center => Owner.Position.Value + _offset;
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
            bool result = false;
            normal = Vector3.Zero;
            depth = 0;

            switch (other)
            {
                case CubeCollider cube:
                    result = this.IsCollideWithCube(cube, out normal, out depth);
                    break;

                case SphereCollider sphere:
                    result = this.IsCollideWithSphere(sphere, out normal, out depth);
                    break;

                default:
                    result = false;
                    break;
            }

            return result;
        }
    }
}
