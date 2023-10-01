using NetCoreMMOServer.Framework.Components;

namespace NetCoreMMOServer.Physics
{
    public class ColliderComponent : Component
    {
        private Collider _collider;

        public ColliderComponent(Collider collider)
        {
            _collider = collider;
        }

        public Collider Collider
        {
            get { return _collider; }
        }
    }
}
