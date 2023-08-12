using NetCoreMMOServer.Physics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace NetCoreMMOServer.Network.Component.Physics
{
    internal static class CollisionAlgorithm
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCollideWithCube(this CubeCollider cubeA, in CubeCollider cubeB, out Vector3 normal, out float depth)
        {
            depth = float.MaxValue;
            normal = Vector3.Zero;

            float atobX = cubeA.MaxPosition.X - cubeB.MinPosition.X;
            if (atobX <= 0) return false;
            normal = new Vector3(1, 0, 0);
            depth = atobX;

            float atobY = cubeA.MaxPosition.Y - cubeB.MinPosition.Y;
            if (atobY <= 0) return false;
            if (atobY < depth)
            {
                normal = new Vector3(0, 1, 0);
                depth = atobY;
            }

            float atobZ = cubeA.MaxPosition.Z - cubeB.MinPosition.Z;
            if (atobZ <= 0) return false;
            if (atobY < depth)
            {
                normal = new Vector3(0, 0, 1);
                depth = atobZ;
            }

            float btoaX = cubeB.MaxPosition.X - cubeA.MinPosition.X;
            if (btoaX <= 0) return false;
            if (btoaX < depth)
            {
                normal = new Vector3(-1, 0, 0);
                depth = btoaX;
            }

            float btoaY = cubeB.MaxPosition.Y - cubeA.MinPosition.Y;
            if (btoaY <= 0) return false;
            if (btoaY < depth)
            {
                normal = new Vector3(0, -1, 0);
                depth = btoaY;
            }

            float btoaZ = cubeB.MaxPosition.Z - cubeA.MinPosition.Z;
            if (btoaZ <= 0) return false;
            if (btoaZ < depth)
            {
                normal = new Vector3(0, 0, -1);
                depth = btoaZ;
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCollideWithSphere(this CubeCollider cube, in SphereCollider sphere, out Vector3 normal, out float depth)
        {
            bool result = sphere.IsCollideWithCube(cube, out normal, out depth);
            normal *= -1;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCollideWithCube(this SphereCollider sphere, in CubeCollider cube, out Vector3 normal, out float depth)
        {
            depth = float.MaxValue;
            normal = Vector3.Zero;

            Vector3 centerA = sphere.Center;
            Vector3 centerB = cube.Center;

            if (!IsIntersectPointCube(centerA, centerB, cube.Size + Vector3.One * sphere.Diameter))
            {
                return false;
            }

            float closestVertexX = float.MaxValue;
            float closestVertexY = float.MaxValue;
            float closestVertexZ = float.MaxValue;

            bool includeX = false;
            bool includeY = false;
            bool includeZ = false;

            if (centerA.X <= cube.MinPosition.X)
            {
                closestVertexX = cube.MinPosition.X;
            }
            else if(centerA.X >= cube.MaxPosition.X)
            {
                closestVertexX = cube.MaxPosition.X;
            }
            else
            {
                closestVertexX = sphere.Center.X;
                includeX = true;
            }

            if (centerA.Y <= cube.MinPosition.Y)
            {
                closestVertexY = cube.MinPosition.Y;
            }
            else if(centerA.Y >= cube.MaxPosition.Y)
            {
                closestVertexY = cube.MaxPosition.Y;
            }
            else
            {
                closestVertexY = sphere.Center.Y;
                includeY = true;
            }

            if (centerA.Z <= cube.MinPosition.Z)
            {
                closestVertexZ = cube.MinPosition.Z;
            }
            else if (centerA.Z >= cube.MaxPosition.Z)
            {
                closestVertexZ = cube.MaxPosition.Z;
            }
            else
            {
                closestVertexZ = sphere.Center.Z;
                includeZ = true;
            }

            Vector3 MinPosition = centerB - (cube.Size + Vector3.One * sphere.Diameter) * 0.5f;
            Vector3 MaxPosition = centerB + (cube.Size + Vector3.One * sphere.Diameter) * 0.5f;

            if(includeX && includeY)
            {
                if(centerA.Z < centerB.Z)
                {
                    float d = centerA.Z - MinPosition.Z;
                    if (d < depth)
                    {
                        depth = d;
                        normal = new Vector3(0, 0, 1);
                    }
                }
                else
                {
                    float d = MaxPosition.Z - centerA.Z;
                    if (d < depth)
                    {
                        depth = d;
                        normal = new Vector3(0, 0, -1);
                    }
                }
            }

            if(includeX && includeZ)
            {
                if (centerA.Y < centerB.Y)
                {
                    float d = centerA.Y - MinPosition.Y;
                    if (d < depth)
                    {
                        depth = d;
                        normal = new Vector3(0, 1, 0);
                    }
                }
                else
                {
                    float d = MaxPosition.Y - centerA.Y;
                    if (d < depth)
                    {
                        depth = d;
                        normal = new Vector3(0, -1, 0);
                    }
                }
            }

            if (includeY && includeZ)
            {
                if (centerA.X < centerB.X)
                {
                    float d = centerA.X - MinPosition.X;
                    if (d < depth)
                    {
                        depth = d;
                        normal = new Vector3(1, 0, 0);
                    }
                }
                else
                {
                    float d = MaxPosition.X - centerA.X;
                    if (d < depth)
                    {
                        depth = d;
                        normal = new Vector3(-1, 0, 0);
                    }
                }
            }

            if (depth < float.MaxValue)
            {
                return true;
            }

            Vector3 closestCorner = new Vector3(closestVertexX, closestVertexY, closestVertexZ);
            return IsIntersectPointCircle(closestCorner, centerA, sphere.Radius, out normal, out depth);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCollideWithSphere(this SphereCollider sphereA, in SphereCollider sphereB, out Vector3 normal, out float depth)
        {
            if (sphereA.Center == sphereB.Center)
            {
                normal = new Vector3(1, 0, 0);
                depth = sphereA.Radius;
                return true;
            }

            float distance = Vector3.Distance(sphereA.Center, sphereB.Center);
            float totRadius = sphereA.Radius + sphereB.Radius;

            if(distance < totRadius)
            {
                normal = Vector3.Normalize(sphereB.Center - sphereA.Center);
                depth = totRadius - distance;
                return true;
            }

            normal = Vector3.Zero;
            depth = 0f;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsIntersectPointCube(in Vector3 point, in Vector3 center, in Vector3 size)
        {
            Vector3 MinPosition = center - size * 0.5f;
            Vector3 MaxPosition = center + size * 0.5f;

            if (point.X >= MinPosition.X &&
                point.X <= MaxPosition.X &&
                point.Y >= MinPosition.Y &&
                point.Y <= MaxPosition.Y &&
                point.Z >= MinPosition.Z &&
                point.Z <= MaxPosition.Z)
            {
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsIntersectPointCircle(in Vector3 point, in Vector3 circleCenter, float radius, out Vector3 normal, out float depth)
        {
            if (point == circleCenter)
            {
                normal = new Vector3(1, 0, 0);
                depth = radius;
                return true;
            }

            float distance = Vector3.Distance(point, circleCenter);
            if (distance < radius)
            {
                normal = Vector3.Normalize(point - circleCenter);
                depth = radius - distance;
                return true;
            }

            normal = Vector3.Zero;
            depth = 0f;
            return false;
        }
    }
}
