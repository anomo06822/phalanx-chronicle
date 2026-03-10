using System;
using System.Collections.Generic;

namespace PhalanxChronicle.Core
{
    [Serializable]
    public readonly struct GridPosition : IEquatable<GridPosition>
    {
        public GridPosition(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }

        public int Y { get; }

        public int ManhattanDistance(GridPosition other)
        {
            return Math.Abs(X - other.X) + Math.Abs(Y - other.Y);
        }

        public IEnumerable<GridPosition> GetOrthogonalNeighbors()
        {
            yield return new GridPosition(X + 1, Y);
            yield return new GridPosition(X - 1, Y);
            yield return new GridPosition(X, Y + 1);
            yield return new GridPosition(X, Y - 1);
        }

        public bool Equals(GridPosition other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is GridPosition other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X * 397) ^ Y;
            }
        }

        public override string ToString()
        {
            return $"({X},{Y})";
        }

        public static bool operator ==(GridPosition left, GridPosition right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GridPosition left, GridPosition right)
        {
            return !left.Equals(right);
        }
    }
}
