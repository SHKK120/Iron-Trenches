using System;

namespace IronTrenches.Core
{
    public sealed class BuildingPlacementRequest
    {
        public BuildingPlacementRequest(
            string siteId,
            string builderFactionId,
            string targetSectorId,
            string buildingTypeId,
            WorldPoint position)
        {
            SiteId = RequireId(siteId, nameof(siteId));
            BuilderFactionId = RequireId(builderFactionId, nameof(builderFactionId));
            TargetSectorId = RequireId(targetSectorId, nameof(targetSectorId));
            BuildingTypeId = RequireId(buildingTypeId, nameof(buildingTypeId));
            Position = position;
        }

        public string SiteId { get; }

        public string BuilderFactionId { get; }

        public string TargetSectorId { get; }

        public string BuildingTypeId { get; }

        public WorldPoint Position { get; }

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
