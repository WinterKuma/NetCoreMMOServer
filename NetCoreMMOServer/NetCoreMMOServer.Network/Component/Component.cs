namespace NetCoreMMOServer.Network.Component
{
    public class Component
    {
        private EntityDataBase _owner;

        public EntityDataBase Owner => _owner;

        public void SetEntityDataBase(EntityDataBase owner)
        {
            _owner = owner;
        }
    }
}
