using System;

namespace IronTrenches.Core
{
    public sealed class TerrainMovementProfile
    {
        public TerrainMovementProfile(string terrainId, float movementMultiplier)
        {
            if (string.IsNullOrWhiteSpace(terrainId))
            {
                throw new ArgumentException("A terrain id cannot be empty.", nameof(terrainId));
            }

            if (!IsFinitePositive(movementMultiplier))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(movementMultiplier),
                    "A movement multiplier must be finite and greater than zero.");
            }

            TerrainId = terrainId;
            MovementMultiplier = movementMultiplier;
        }

        public string TerrainId { get; }

        public float MovementMultiplier { get; }

        private static bool IsFinitePositive(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) && value > 0f;
        }
    }
}
