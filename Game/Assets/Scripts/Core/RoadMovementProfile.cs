using System;

namespace IronTrenches.Core
{
    public sealed class RoadMovementProfile
    {
        public RoadMovementProfile(float movementMultiplier)
        {
            if (float.IsNaN(movementMultiplier)
                || float.IsInfinity(movementMultiplier)
                || movementMultiplier <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(movementMultiplier),
                    "A road movement multiplier must be finite and greater than zero.");
            }

            MovementMultiplier = movementMultiplier;
        }

        public float MovementMultiplier { get; }
    }
}
