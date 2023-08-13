using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetCoreMMOServer
{
    public static class ZoneOption
    {
        public static readonly int ZoneCountX = 5;
        public static readonly int ZoneCountY = 5;
        public static readonly int ZoneCountZ = 5;

        public static readonly float ZoneWidth = 5.0f;
        public static readonly float ZoneHeight = 5.0f;
        public static readonly float ZoneDepth = 5.0f;

        public static readonly float InverseZoneWidth = 1 / ZoneWidth;
        public static readonly float InverseZoneHeight = 1 / ZoneHeight;
        public static readonly float InverseZoneDepth = 1 / ZoneDepth;

        public static readonly float TotalZoneHalfWidth = ZoneCountX * 0.5f * ZoneWidth;
        public static readonly float TotalZoneHalfHeight = ZoneCountY * 0.5f * ZoneHeight;
        public static readonly float TotalZoneHalfDepth = ZoneCountZ * 0.5f * ZoneDepth;

        public static readonly int AddZoneRangeX = 2;
        public static readonly int AddZoneRangeY = 2;
        public static readonly int AddZoneRangeZ = 2;

        public static readonly int RemoveZoneRangeX = 3;
        public static readonly int RemoveZoneRangeY = 3;
        public static readonly int RemoveZoneRangeZ = 3;
    }
}
