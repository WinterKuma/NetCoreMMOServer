using NetCoreMMOServer.Framework.Components;
using NetCoreMMOServer.Physics;
using System.Numerics;

namespace NetCoreMMOServer.Framework
{
    public class Entity
    {
        protected readonly Transform _transform = new Transform();
        protected readonly List<Component> _components;
        protected int _layer = 0;

        public Entity()
        {
            _transform = new Transform();
            _transform.Position = Vector3.Zero;

            _components = new List<Component>(12);
        }

        public Transform Transform => _transform;
        public List<Component> Components => _components;
        public int Layer => _layer;

        public virtual void Update(float dt)
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

        public T? GetComponent<T>() where T : Component
        {
            T? value = null;

            foreach (var component in _components)
            {
                if (component is T t)
                {
                    value = t;
                    break;
                }
            }

            return value;
        }

        public bool TryGetComponent<T>(out T? value) where T : Component
        {
            value = null;

            foreach (var component in _components)
            {
                if (component is T t)
                {
                    value = t;
                    return true;
                }
            }

            return false;
        }
    }
}
