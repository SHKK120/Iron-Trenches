using System;

namespace IronTrenches.Core
{
    public readonly struct FrontlineEdge : IEquatable<FrontlineEdge>
    {
        public FrontlineEdge(string firstSectorId, string secondSectorId)
        {
            firstSectorId = RequireId(firstSectorId, nameof(firstSectorId));
            secondSectorId = RequireId(secondSectorId, nameof(secondSectorId));

            if (string.Equals(firstSectorId, secondSectorId, StringComparison.Ordinal))
            {
                throw new ArgumentException("A frontline edge requires two different sectors.", nameof(secondSectorId));
            }

            if (string.CompareOrdinal(firstSectorId, secondSectorId) < 0)
            {
                FirstSectorId = firstSectorId;
                SecondSectorId = secondSectorId;
            }
            else
            {
                FirstSectorId = secondSectorId;
                SecondSectorId = firstSectorId;
            }
        }

        public string FirstSectorId { get; }

        public string SecondSectorId { get; }

        public bool Equals(FrontlineEdge other)
        {
            return string.Equals(FirstSectorId, other.FirstSectorId, StringComparison.Ordinal)
                && string.Equals(SecondSectorId, other.SecondSectorId, StringComparison.Ordinal);
        }

        public override bool Equals(object? obj)
        {
            return obj is FrontlineEdge other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (StringComparer.Ordinal.GetHashCode(FirstSectorId) * 397)
                    ^ StringComparer.Ordinal.GetHashCode(SecondSectorId);
            }
        }

        public static bool operator ==(FrontlineEdge left, FrontlineEdge right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(FrontlineEdge left, FrontlineEdge right)
        {
            return !left.Equals(right);
        }

        private static string RequireId(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("An id cannot be empty.", parameterName);
            }

            return value;
        }
    }
}
