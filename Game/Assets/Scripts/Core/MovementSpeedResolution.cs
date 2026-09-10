using System;

namespace IronTrenches.Core
{
    public sealed class MovementSpeedResolution
    {
        internal MovementSpeedResolution(
            TerrainMovementProfile terrain,
            bool isOnRoad,
            float movementMultiplier,
            float finalSpeed)
        {
            Terrain = terrain ?? throw new ArgumentNullException(nameof(terrain));
            IsOnRoad = isOnRoad;
            MovementMultiplier = movementMultiplier;
            FinalSpeed = finalSpeed;
        }

        public TerrainMovementProfile Terrain { get; }

        public bool IsOnRoad { get; }

        public float MovementMultiplier { get; }

        public float FinalSpeed { get; }
    }
}
