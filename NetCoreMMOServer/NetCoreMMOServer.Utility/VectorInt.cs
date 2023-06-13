namespace NetCoreMMOServer.Utility
{
    public partial struct Vector2Int : IEquatable<Vector2Int>
    {
        public readonly int X;
        public readonly int Y;

        public Vector2Int()
        {
            X = 0;
            Y = 0;
        }

        public Vector2Int(int x, int y)
        {
            X = x;
            Y = y;
        }

        public Vector2Int(float x, float y)
        {
            X = (int)x;
            Y = (int)y;
        }

        public static Vector2Int operator +(Vector2Int lhs, Vector2Int rhs)
        {
            return new Vector2Int(lhs.X + rhs.X, lhs.Y + rhs.Y);
        }

        public static Vector2Int operator -(Vector2Int lhs, Vector2Int rhs)
        {
            return new Vector2Int(lhs.X - rhs.X, lhs.Y - rhs.Y);
        }

        public static Vector2Int operator *(Vector2Int lhs, int rhs)
        {
            return new Vector2Int(lhs.X * rhs, lhs.Y * rhs);
        }

        public static Vector2Int operator /(Vector2Int lhs, int rhs)
        {
            return new Vector2Int(lhs.X / rhs, lhs.Y / rhs);
        }

        public static bool operator ==(Vector2Int lhs, Vector2Int rhs)
        {
            return lhs.X == rhs.X && lhs.Y == rhs.Y;
        }

        public static bool operator !=(Vector2Int lhs, Vector2Int rhs)
        {
            return lhs.X != rhs.X || lhs.Y != rhs.Y;
        }

        public bool Equals(Vector2Int other)
        {
            return this == other;
        }

        public override bool Equals(object? obj)
        {
            return obj is Vector2Int other && this == other;
        }

        public override int GetHashCode()
        {
            return this.X.GetHashCode() ^ this.Y.GetHashCode();
        }
    }


    public struct Vector3Int
    {
        public readonly int X;
        public readonly int Y;
        public readonly int Z;

        public Vector3Int()
        {
            X = 0;
            Y = 0;
            Z = 0;
        }

        public Vector3Int(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Vector3Int(float x, float y, float z)
        {
            X = (int)x;
            Y = (int)y;
            Z = (int)z;
        }

        public static Vector3Int operator +(Vector3Int lhs, Vector3Int rhs)
        {
            return new Vector3Int(lhs.X + rhs.X, lhs.Y + rhs.Y, lhs.Z + rhs.Z);
        }

        public static Vector3Int operator -(Vector3Int lhs, Vector3Int rhs)
        {
            return new Vector3Int(lhs.X - rhs.X, lhs.Y - rhs.Y, lhs.Z - rhs.Z);
        }

        public static Vector3Int operator *(Vector3Int lhs, int rhs)
        {
            return new Vector3Int(lhs.X * rhs, lhs.Y * rhs, lhs.Z * rhs);
        }

        public static Vector3Int operator /(Vector3Int lhs, int rhs)
        {
            return new Vector3Int(lhs.X / rhs, lhs.Y / rhs, lhs.Z / rhs);
        }

        public static bool operator ==(Vector3Int lhs, Vector3Int rhs)
        {
            return lhs.X == rhs.X && lhs.Y == rhs.Y && lhs.Z == rhs.Z;
        }

        public static bool operator !=(Vector3Int lhs, Vector3Int rhs)
        {
            return lhs.X != rhs.X || lhs.Y != rhs.Y || lhs.Z != rhs.Z;
        }

        public bool Equals(Vector3Int other)
        {
            return this == other;
        }

        public override bool Equals(object? obj)
        {
            return obj is Vector3Int other && this == other;
        }

        public override int GetHashCode()
        {
            return this.X.GetHashCode() ^ this.Y.GetHashCode() ^ this.Z.GetHashCode();
        }
    }

    public struct Vector4Int
    {

    }
}
