using NetCoreMMOServer.Packet;
using System.Numerics;

namespace NetCoreMMOServer.Contents.Entity.Master
{
    public abstract class Master_PlayerEntity_Base : PlayerEntity
    {
        private RPCGetDamagePacket _GetDamagePacket;
        private RPCTest3Packet _Test3Packet;
        public Master_PlayerEntity_Base()
        {
            _GetDamagePacket = new RPCGetDamagePacket();
            _Test3Packet = new RPCTest3Packet();
        }

        public void UpdateDataTablePacket()
        {
            this.UpdateDataTablePacket_Server();
        }

        public void LoadDataTablePacket(EntityDataTable loadDataTable)
        {
            this.LoadDataTablePacket_Server(loadDataTable);
        }

        public override void GetDamage(int damage)
        {
            _GetDamagePacket.damage = damage;
            SendRPCPacket(_GetDamagePacket);
        }

        public override void Test3(int val1, float val2)
        {
            _Test3Packet.val1 = val1;
            _Test3Packet.val2 = val2;
            SendRPCPacket(_Test3Packet);
        }

        public override void ReceiveRPC(in RPCPacket rpcPacket)
        {
            switch (rpcPacket)
            {
                case RPCAttackPacket attack:
                    Attack();
                    break;

                case RPCTestPacket test:
                    Test(test.value);
                    break;

                default:
                    Console.WriteLine($"Error:: Not Found RPC Packet ({rpcPacket})");
                    break;
            }
        }
    }
}
