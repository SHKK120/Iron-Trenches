using System;

namespace IronTrenches.Core
{
    public static class MovementSpeedResolver
    {
        public static MovementSpeedResolution Resolve(
            float baseSpeed,
            WorldPoint point,
            ITerrainMovementResolver terrainResolver,
            RoadNetwork roadNetwork,
            RoadMovementProfile roadProfile)
        {
            if (float.IsNaN(baseSpeed) || float.IsInfinity(baseSpeed) || baseSpeed <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(baseSpeed),
                    "Base speed must be finite and greater than zero.");
            }

            if (!RoadSegment.IsFinite(point))
            {
                throw new ArgumentException("A movement point must be finite.", nameof(point));
            }

            if (terrainResolver == null)
            {
                throw new ArgumentNullException(nameof(terrainResolver));
            }

            if (roadNetwork == null)
            {
                throw new ArgumentNullException(nameof(roadNetwork));
            }

            if (roadProfile == null)
            {
                throw new ArgumentNullException(nameof(roadProfile));
            }

            var terrain = terrainResolver.Resolve(point)
                ?? throw new InvalidOperationException("Terrain resolver returned null.");
            var isOnRoad = roadNetwork.IsPointOnRoad(point);
            var multiplier = isOnRoad
                ? roadProfile.MovementMultiplier
                : terrain.MovementMultiplier;
            return new MovementSpeedResolution(
                terrain,
                isOnRoad,
                multiplier,
                baseSpeed * multiplier);
        }
    }
}
