namespace NetCoreMMOServer.Framework.Components
{
    public class Component
    {
        private NetEntity _owner;

        public NetEntity Owner => _owner;

        public void SetEntityDataBase(NetEntity owner)
        {
            _owner = owner;
        }
    }
}
