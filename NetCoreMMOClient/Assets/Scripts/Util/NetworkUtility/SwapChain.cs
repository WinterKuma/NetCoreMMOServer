namespace NetCoreMMOServer.Utility
{
    public class SwapChain<T> where T : class, new()
    {
        private T[] _buffers;
        private byte _bufferIndex;

        public SwapChain()
        {
            _buffers = new T[2] { new(), new() };
            _bufferIndex = 0;
        }

        public T CurrentBuffer => _buffers[_bufferIndex];
        public T Swap()
        {
            byte lastBuffer = _bufferIndex;
            lock (_buffers[lastBuffer])
            {
                if (_bufferIndex == 0)
                {
                    _bufferIndex = 1;
                }
                else
                {
                    _bufferIndex = 0;
                }

            }
            return _buffers[lastBuffer];
        }
    }
}
