using NetCoreMMOServer.Utility;
using System.Numerics;

namespace NetCoreMMOServer
{
    public static class ZoneOption
    {
        public static readonly int ZoneCountX = 5;
        public static readonly int ZoneCountY = 3;
        public static readonly int ZoneCountZ = 5;
        public static readonly Vector3Int ZoneCountXYZInt = new Vector3Int(ZoneCountX, ZoneCountY, ZoneCountZ);
        public static readonly Vector3 ZoneCountXYZ = new Vector3(ZoneCountX, ZoneCountY, ZoneCountZ);

        public static readonly float ZoneWidth = 3.0f;
        public static readonly float ZoneHeight = 3.0f;
        public static readonly float ZoneDepth = 3.0f;
        public static readonly Vector3 ZoneSize = new Vector3(ZoneWidth, ZoneHeight, ZoneDepth);

        public static readonly float InverseZoneWidth = 1 / ZoneWidth;
        public static readonly float InverseZoneHeight = 1 / ZoneHeight;
        public static readonly float InverseZoneDepth = 1 / ZoneDepth;
        public static readonly Vector3 InverseZoneSize = new Vector3(InverseZoneWidth, InverseZoneHeight, InverseZoneDepth);

        public static readonly float TotalZoneWidth = ZoneCountX * ZoneWidth;
        public static readonly float TotalZoneHeight = ZoneCountY * ZoneHeight;
        public static readonly float TotalZoneDepth = ZoneCountZ * ZoneDepth;
        public static readonly Vector3 TotalZoneSize = new Vector3(TotalZoneWidth, TotalZoneHeight, TotalZoneDepth);

        public static readonly float TotalZoneHalfWidth = ZoneCountX * 0.5f * ZoneWidth;
        public static readonly float TotalZoneHalfHeight = ZoneCountY * 0.5f * ZoneHeight;
        public static readonly float TotalZoneHalfDepth = ZoneCountZ * 0.5f * ZoneDepth;
        public static readonly Vector3 TotalZoneHalfSize = new Vector3(TotalZoneHalfWidth, TotalZoneHalfHeight, TotalZoneHalfDepth);

        public static readonly int AddZoneRangeX = 3;
        public static readonly int AddZoneRangeY = 2;
        public static readonly int AddZoneRangeZ = 3;
        public static readonly Vector3Int AddZoneRangeXYZ = new Vector3Int(AddZoneRangeX, AddZoneRangeY, AddZoneRangeZ);

        public static readonly int RemoveZoneRangeX = 4;
        public static readonly int RemoveZoneRangeY = 3;
        public static readonly int RemoveZoneRangeZ = 4;
        public static readonly Vector3Int RemoveZoneRangeXYZ = new Vector3Int(RemoveZoneRangeX, RemoveZoneRangeY, RemoveZoneRangeZ);
    }
}
