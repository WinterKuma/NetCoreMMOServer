using NetCoreMMOServer.Network.Component;
using System.Numerics;

namespace NetCoreMMOServer.Physics
{
    public class RigidBody : Component
    {
        private Vector3 _velocity;
        private Vector3 _gravity;
        private float _mass;

        private bool _isStatic = false;

        public bool IsStatic => _isStatic;

        public Vector3 Velocity
        {
            get { return _velocity; }
            set { _velocity = value; }
        }

        public Vector3 Gravity
        {
            get { return _gravity; }
            set { _gravity = value; }
        }
    }
}
