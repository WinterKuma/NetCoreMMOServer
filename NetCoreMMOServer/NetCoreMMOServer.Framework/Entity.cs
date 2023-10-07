﻿using NetCoreMMOServer.Framework.Components;
using NetCoreMMOServer.Physics;
using System.Numerics;

namespace NetCoreMMOServer.Framework
{
    public class Entity
    {
        protected readonly Transform _transform = new Transform();
        protected readonly List<Component> _components;

        public Entity()
        {
            _transform = new Transform();
            _transform.Position = Vector3.Zero;

            _components = new List<Component>(12);
        }

        public Transform Transform => _transform;
        public List<Component> Components => _components;

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
    }
}
