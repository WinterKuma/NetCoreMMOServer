using System.Numerics;

namespace NetCoreMMOServer
{
    public static class PhysicsOption
    {
        public static readonly int StepCount = 10;
        public static readonly float InverseStepCount = 1f / StepCount;

        public static readonly Vector3 Gravity = new Vector3(0.0f, -9.81f, 0.0f);
    }
}
