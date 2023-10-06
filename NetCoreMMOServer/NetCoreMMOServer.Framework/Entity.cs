using NetCoreMMOServer.Framework.Components;
using NetCoreMMOServer.Physics;

namespace NetCoreMMOServer.Framework
{
    public class Entity
    {
        protected readonly Transform _transform = new Transform();
        protected readonly List<Component> _components;

        public Entity()
        {
            _transform = new Transform();
            _components = new List<Component>(12);
        }

        public Transform Transform => _transform;
        public List<Component> Components => _components;

        public void Update(float dt)
        {

        }

        public void FixedUpdate(float dt)
        {

        }

        public virtual void OnTriggerEnter()
        {

        }

        public virtual void OnCollisionEnter()
        {

        }
    }
}
