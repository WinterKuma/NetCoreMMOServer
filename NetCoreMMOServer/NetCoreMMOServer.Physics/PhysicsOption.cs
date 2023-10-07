using System.Numerics;

namespace NetCoreMMOServer
{
    public static class PhysicsOption
    {
        public static readonly float IntervalDelta = 0.01f;

        public static readonly Vector3 Gravity = new Vector3(0.0f, -9.81f, 0.0f);
    }
}
