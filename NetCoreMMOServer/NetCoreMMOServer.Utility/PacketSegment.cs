using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace NetCoreMMOServer.Utility
{
    public class PacketSegment<T> : ReadOnlySequenceSegment<T>
    {
        public PacketSegment(T[] array)
        {
            Memory = array;
        }

        public PacketSegment<T> Add(T[] array)
        {
            var segment = new PacketSegment<T>(array);
            segment.RunningIndex = RunningIndex + Memory.Length;

            Next = segment;
            return segment;
        }
    }
}
