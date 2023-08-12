using NetCoreMMOServer.Network.Component.Physics;
using System.Numerics;

namespace NetCoreMMOServer.Physics
{
    public class CubeCollider : Collider
    {
        private Vector3 _offset;
        private Vector3 _size;

        public Vector3 Offset
        {
            get { return _offset; }
            set { _offset = value; }
        }

        public Vector3 Size
        {
            get { return _size; }
            set { _size = value; }
        }

        public Vector3 Center => Owner.Position.Value + _offset;
        public Vector3 MaxPosition => Center + Size;
        public Vector3 MinPosition => Center - Size;

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
                    break;

                default:
                    result = false;
                    break;
            }

            return result;
        }
    }
}
