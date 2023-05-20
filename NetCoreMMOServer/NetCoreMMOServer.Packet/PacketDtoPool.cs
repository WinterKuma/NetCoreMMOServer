using NetCoreMMOServer.Utility;
using System.Runtime.CompilerServices;

namespace NetCoreMMOServer.Packet
{
    public static class PacketPool
    {
        private static class Cache<T> where T : IMPacket, new()
        {
            public static ConcurrentPool<T> _pakcetPool = new();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IMPacket Get<T>() where T : IMPacket, new()
        {
            return Cache<T>._pakcetPool.Get();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Return<T>(T packet) where T : IMPacket, new()
        {
            Cache<T>._pakcetPool.Return(packet);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReturnPacket(IMPacket packet)
        {
            switch (packet)
            {
                case EntityDto entityDto:
                    Return(entityDto);
                    break;
                case MoveDto moveDto:
                    Return(moveDto);
                    break;
            }
        }
    }
}
