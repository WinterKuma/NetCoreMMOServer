using NetCoreMMOServer.Packet;
using System.Numerics;

namespace NetCoreMMOServer.Contents.Entity.Remote
{
    public abstract class Remote_PlayerEntity_Base : PlayerEntity
    {
        private RPCAttackPacket _AttackPacket;
        private RPCTestPacket _TestPacket;

        public Remote_PlayerEntity_Base()
        {
            _AttackPacket = new RPCAttackPacket();
            _TestPacket = new RPCTestPacket();
        }

        public void UpdateDataTablePacket()
        {
            this.UpdateDataTablePacket_Client();
        }

        public void LoadDataTablePacket(EntityDataTable loadDataTable)
        {
            this.LoadDataTablePacket_Client(loadDataTable);
        }

        public override void Attack()
        {
            SendRPCPacket(_AttackPacket);
        }

        public override void Test(Vector3 value)
        {
            _TestPacket.value = value;
            SendRPCPacket(_TestPacket);
        }

        public override void ReceiveRPC(in RPCPacket rpcPacket)
        {
            switch (rpcPacket)
            {
                case RPCGetDamagePacket getDamage:
                    GetDamage(getDamage.damage);
                    break;

                case RPCTest3Packet test3:
                    Test3(test3.val1, test3.val2);
                    break;

                default:
                    Console.WriteLine($"Error:: Not Found RPC Packet ({rpcPacket})");
                    break;
            }
        }
    }
}
