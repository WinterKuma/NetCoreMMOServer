using NetCoreMMOServer.Network.Components;
using System.Numerics;

namespace NetCoreMMOServer.Physics
{
    public class Transform : Component
    {
        private Vector3 _position;

        public Vector3 Position
        {
            get { return _position; }
            set { _position = value; }
        }
    }
}
