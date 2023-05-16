using NetCoreMMOServer.Utility;

namespace NetCoreMMOServer.Packet
{
    public static class PacketDtoPoolProvider
    {
        private static class Cache<T> where T : IMPacket, new()
        {
            public static PacketDtoPool<T> dtoPool = new();
        }

        public static PacketDtoPool<T> GetDtoPool<T>() where T : IMPacket, new()
        {
            return Cache<T>.dtoPool;
        }

        public static IMPacket GetDto<T>() where T : IMPacket, new()
        {
            return Cache<T>.dtoPool.GetDto();
        }

        public static void ReturnDto<T>(T packet) where T : IMPacket, new()
        {
            Cache<T>.dtoPool.ReturnDto(packet);
        }

        public static void ReturnDto(IMPacket packet)
        {
            switch (packet)
            {
                case EntityDto entityDto:
                    ReturnDto(entityDto);
                    break;
                case MoveDto moveDto:
                    ReturnDto(moveDto);
                    break;
            }
        }
    }

    public class PacketDtoPool<T> where T : IMPacket, new()
    {
        private readonly ConcurrentPool<T> dtoPool = new();

        public ConcurrentPool<T> GetDtoPool()
        {
            return dtoPool;
        }

        public IMPacket GetDto()
        {
            return dtoPool.Get();
        }

        public void ReturnDto(T packet)
        {
            dtoPool.Return(packet);
        }
    }
}
