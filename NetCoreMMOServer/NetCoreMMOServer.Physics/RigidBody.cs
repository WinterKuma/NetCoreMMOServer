using System.Numerics;

namespace NetCoreMMOServer.Physics
{
    public class RigidBody
    {
        private Vector3 _velocity;
        private Vector3 _prevVelocity;
        private Vector3 _nextVelocity;

        private float _mass;
        private float _invMass;

        private bool _isStatic;
        private Transform? _transform = null;

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

        public Vector3 PrevVelcotiy
        {
            get { return _prevVelocity; }
            set { _prevVelocity = value; }
        }

        public Vector3 NextVelocity
        {
            get { return _nextVelocity; }
            set { _nextVelocity = value; }
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

        public Transform? Transform
        {
            get { return _transform; }
            set { _transform = value; }
        }
    }
}
