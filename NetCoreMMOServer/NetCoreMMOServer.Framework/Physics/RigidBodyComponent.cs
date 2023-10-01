using NetCoreMMOServer.Framework.Components;
using System.Numerics;

namespace NetCoreMMOServer.Physics
{
    public class RigidBodyComponent : Component
    {
        private RigidBody _rigidBody;

        public RigidBodyComponent(float mass = 1.0f, bool isStatic = false, Vector3 velocity = default)
        {
            _rigidBody = new RigidBody(mass, isStatic, velocity);
        }

        public RigidBodyComponent(RigidBody rigidBody)
        {
            _rigidBody = rigidBody;
        }

        public RigidBody RigidBody
        {
            get { return _rigidBody; }
        }
    }
}
