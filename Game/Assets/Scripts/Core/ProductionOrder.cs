using System;

namespace IronTrenches.Core
{
    public sealed class ProductionOrder
    {
        internal ProductionOrder(
            ProductionRequest request,
            ProductionDefinition definition)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            OrderId = request.OrderId;
            ReinforcementId = request.ReinforcementId;
            FacilityBuildingId = request.FacilityBuildingId;
            RequestedByFactionId = request.RequestedByFactionId;
            DestinationSectorId = request.DestinationSectorId;
            DestinationPosition = request.DestinationPosition;
        }

        public string OrderId { get; }

        public string ReinforcementId { get; }

        public string FacilityBuildingId { get; }

        public string RequestedByFactionId { get; }

        public string UnitTypeId => Definition.UnitTypeId;

        public string DestinationSectorId { get; }

        public WorldPoint DestinationPosition { get; }

        public float ProgressSeconds { get; private set; }

        public float RemainingSeconds => Math.Max(0f, Definition.ProductionSeconds - ProgressSeconds);

        internal ProductionDefinition Definition { get; }

        internal void AddProgress(float seconds)
        {
            ProgressSeconds = Math.Min(
                Definition.ProductionSeconds,
                ProgressSeconds + seconds);
        }
    }
}
