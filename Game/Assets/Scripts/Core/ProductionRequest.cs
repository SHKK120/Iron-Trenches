using System;

namespace IronTrenches.Core
{
    public sealed class ProductionRequest
    {
        public ProductionRequest(
            string orderId,
            string reinforcementId,
            string facilityBuildingId,
            string requestedByFactionId,
            string unitTypeId,
            string destinationSectorId,
            WorldPoint destinationPosition)
        {
            OrderId = RequireId(orderId, nameof(orderId));
            ReinforcementId = RequireId(reinforcementId, nameof(reinforcementId));
            FacilityBuildingId = RequireId(facilityBuildingId, nameof(facilityBuildingId));
            RequestedByFactionId = RequireId(requestedByFactionId, nameof(requestedByFactionId));
            UnitTypeId = RequireId(unitTypeId, nameof(unitTypeId));
            DestinationSectorId = RequireId(destinationSectorId, nameof(destinationSectorId));
            DestinationPosition = destinationPosition;
        }

        public string OrderId { get; }

        public string ReinforcementId { get; }

        public string FacilityBuildingId { get; }

        public string RequestedByFactionId { get; }

        public string UnitTypeId { get; }

        public string DestinationSectorId { get; }

        public WorldPoint DestinationPosition { get; }

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
