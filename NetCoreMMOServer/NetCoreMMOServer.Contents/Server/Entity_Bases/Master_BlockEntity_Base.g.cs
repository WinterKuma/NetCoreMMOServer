using NetCoreMMOServer.Packet;
using System.Numerics;

namespace NetCoreMMOServer.Contents.Entity.Master
{
    public abstract class Master_BlockEntity_Base : BlockEntity
    {
        public Master_BlockEntity_Base()
        {
        }

        public void UpdateDataTablePacket()
        {
            this.UpdateDataTablePacket_Server();
        }

        public void LoadDataTablePacket(EntityDataTable loadDataTable)
        {
            this.LoadDataTablePacket_Server(loadDataTable);
        }

        public override void ReceiveRPC(in RPCPacket rpcPacket)
        {
            switch (rpcPacket)
            {
                default:
                    Console.WriteLine($"Error:: Not Found RPC Packet ({rpcPacket})");
                    break;
            }
        }
    }
}
