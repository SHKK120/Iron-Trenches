using System;

namespace IronTrenches.Core
{
    public sealed class ReadyReinforcement
    {
        internal ReadyReinforcement(ProductionOrder order, BuildingState sourceBuilding)
        {
            if (order == null)
            {
                throw new ArgumentNullException(nameof(order));
            }

            if (sourceBuilding == null)
            {
                throw new ArgumentNullException(nameof(sourceBuilding));
            }

            ReinforcementId = order.ReinforcementId;
            OrderId = order.OrderId;
            FactionId = order.RequestedByFactionId;
            UnitTypeId = order.UnitTypeId;
            SourceBuildingId = sourceBuilding.BuildingId;
            SourceSectorId = sourceBuilding.SectorId;
            SourcePosition = sourceBuilding.Position;
            DestinationSectorId = order.DestinationSectorId;
            DestinationPosition = order.DestinationPosition;
            BaseMovementSpeed = order.Definition.BaseMovementSpeed;
        }

        public string ReinforcementId { get; }

        public string OrderId { get; }

        public string FactionId { get; }

        public string UnitTypeId { get; }

        public string SourceBuildingId { get; }

        public string SourceSectorId { get; }

        public WorldPoint SourcePosition { get; }

        public string DestinationSectorId { get; }

        public WorldPoint DestinationPosition { get; }

        public float BaseMovementSpeed { get; }
    }
}
