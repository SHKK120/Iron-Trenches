using System;

namespace IronTrenches.Core
{
    public sealed class BuildingDefinition
    {
        public BuildingDefinition(string buildingTypeId, long cost, float footprintRadius)
        {
            if (string.IsNullOrWhiteSpace(buildingTypeId))
            {
                throw new ArgumentException("An id cannot be empty.", nameof(buildingTypeId));
            }

            if (cost < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cost), "Building cost cannot be negative.");
            }

            if (float.IsNaN(footprintRadius)
                || float.IsInfinity(footprintRadius)
                || footprintRadius <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(footprintRadius),
                    "Footprint radius must be finite and greater than zero.");
            }

            BuildingTypeId = buildingTypeId;
            Cost = cost;
            FootprintRadius = footprintRadius;
        }

        public string BuildingTypeId { get; }

        public long Cost { get; }

        public float FootprintRadius { get; }
    }
}
