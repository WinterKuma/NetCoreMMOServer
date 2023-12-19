using NetCoreMMOServer.Packet;
using System.Numerics;

namespace NetCoreMMOServer.Contents.Entity.Remote
{
    public abstract class Remote_BlockEntity_Base : BlockEntity
    {

        public Remote_BlockEntity_Base()
        {
        }

        public void UpdateDataTablePacket()
        {
            this.UpdateDataTablePacket_Client();
        }

        public void LoadDataTablePacket(EntityDataTable loadDataTable)
        {
            this.LoadDataTablePacket_Client(loadDataTable);
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
