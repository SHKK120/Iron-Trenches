using System;

namespace IronTrenches.Core
{
    public enum ReinforcementRouteSurface
    {
        Road,
        Offroad
    }

    public sealed class ReinforcementRouteLeg
    {
        public ReinforcementRouteLeg(
            WorldPoint start,
            WorldPoint end,
            ReinforcementRouteSurface surface,
            float movementMultiplier)
        {
            if (!RoadSegment.IsFinite(start) || !RoadSegment.IsFinite(end))
            {
                throw new ArgumentException("Route leg endpoints must be finite.");
            }

            if (RoadSegment.PointsEqual(start, end))
            {
                throw new ArgumentException("Route leg endpoints must be different.", nameof(end));
            }

            if (float.IsNaN(movementMultiplier)
                || float.IsInfinity(movementMultiplier)
                || movementMultiplier <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(movementMultiplier),
                    "Movement multiplier must be finite and greater than zero.");
            }

            Start = start;
            End = end;
            Surface = surface;
            MovementMultiplier = movementMultiplier;

            var deltaX = (double)end.X - start.X;
            var deltaZ = (double)end.Z - start.Z;
            Distance = (float)Math.Sqrt((deltaX * deltaX) + (deltaZ * deltaZ));
        }

        public WorldPoint Start { get; }

        public WorldPoint End { get; }

        public ReinforcementRouteSurface Surface { get; }

        public float Distance { get; }

        public float MovementMultiplier { get; }

        public float EstimateTravelTime(float baseMovementSpeed)
        {
            if (float.IsNaN(baseMovementSpeed)
                || float.IsInfinity(baseMovementSpeed)
                || baseMovementSpeed <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(baseMovementSpeed));
            }

            return Distance / (baseMovementSpeed * MovementMultiplier);
        }
    }
}
