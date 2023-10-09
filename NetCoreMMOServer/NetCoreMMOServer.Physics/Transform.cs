using System.Numerics;

namespace NetCoreMMOServer.Physics
{
    public class Transform
    {
        private Vector3 _position;
        private Quaternion _rotation;

        public Vector3 Position
        {
            get { return _position; }
            set { _position = value; }
        }

        public Quaternion Rotation
        {
            get { return _rotation; }
            set { _rotation = value; }
        }
    }
}
