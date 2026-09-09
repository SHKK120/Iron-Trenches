using System;

namespace IronTrenches.Core
{
    public sealed class ConstructionSite
    {
        public ConstructionSite(
            string siteId,
            string buildingTypeId,
            string ownerId,
            string sectorId,
            WorldPoint position,
            float footprintRadius)
        {
            SiteId = RequireId(siteId, nameof(siteId));
            BuildingTypeId = RequireId(buildingTypeId, nameof(buildingTypeId));
            OwnerId = RequireId(ownerId, nameof(ownerId));
            SectorId = RequireId(sectorId, nameof(sectorId));

            if (!IsFinite(position))
            {
                throw new ArgumentException("Construction site position must be finite.", nameof(position));
            }

            if (float.IsNaN(footprintRadius)
                || float.IsInfinity(footprintRadius)
                || footprintRadius <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(footprintRadius),
                    "Footprint radius must be finite and greater than zero.");
            }

            Position = position;
            FootprintRadius = footprintRadius;
        }

        public string SiteId { get; }

        public string BuildingTypeId { get; }

        public string OwnerId { get; }

        public string SectorId { get; }

        public WorldPoint Position { get; }

        public float FootprintRadius { get; }

        private static bool IsFinite(WorldPoint point)
        {
            return !float.IsNaN(point.X)
                && !float.IsInfinity(point.X)
                && !float.IsNaN(point.Z)
                && !float.IsInfinity(point.Z);
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
