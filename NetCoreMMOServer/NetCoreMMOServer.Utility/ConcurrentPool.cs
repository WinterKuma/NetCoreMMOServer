using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetCoreMMOServer.Utility
{
    public class ConcurrentPool<T> where T : new()
    {
        private ConcurrentBag<T> _pool = new();

        public T Get()
        {
            if(_pool.TryTake(out var item)) 
            { 
                return item; 
            }
            else
            {
                return new T();
            }
        }

        public void Return(T item)
        {
            _pool.Add(item);
        }
    }
}
