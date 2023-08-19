using NetCoreMMOServer.Network.Component;
using System.Numerics;

namespace NetCoreMMOServer.Physics
{
    public class RigidBody : Component
    {
        private Vector3 _velocity;
        private float _mass;
        private float _invMass;

        private bool _isStatic;

        public RigidBody(float mass = 1f, bool isStatic = false, Vector3 velocity = default)
        {
            _mass = mass;
            if(isStatic)
            {
                _invMass = 0f;
            }
            else
            {
                _invMass = 1 / _mass;
            }

            _isStatic = isStatic;
            _velocity = velocity;
        }

        public bool IsStatic => _isStatic;

        public Vector3 Velocity
        {
            get { return _velocity; }
            set { _velocity = value; }
        }

        public float Mass
        {
            get { return _mass; }
            set 
            { 
                _mass = value;
                _invMass = 1 / _mass;
            }
        }

        public float InvMass
        {
            get { return _invMass; }
        }
    }
}
