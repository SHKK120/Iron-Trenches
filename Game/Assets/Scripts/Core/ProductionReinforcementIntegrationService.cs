using System;
using System.Collections.Generic;

namespace IronTrenches.Core
{
    public static class ProductionReinforcementIntegrationService
    {
        public static ProductionReinforcementIntegrationResult Dispatch(
            ReadyReinforcement reinforcement,
            IEnumerable<BuildingState> completedBuildings,
            TerritoryGraph territory,
            SupplyNetworkSnapshot supplySnapshot,
            ReinforcementRoutePlanner routePlanner,
            ICollection<ReadyReinforcement> readyReinforcements,
            ICollection<ReinforcementTransit> existingTransits)
        {
            return DispatchInternal(
                reinforcement,
                completedBuildings,
                territory,
                supplySnapshot,
                routePlanner,
                null,
                null,
                readyReinforcements,
                existingTransits);
        }

        public static ProductionReinforcementIntegrationResult Dispatch(
            ReadyReinforcement reinforcement,
            IEnumerable<BuildingState> completedBuildings,
            TerritoryGraph territory,
            SupplyNetworkSnapshot supplySnapshot,
            ReinforcementRoutePlanner routePlanner,
            IEnumerable<RouteThreatSource> threatSources,
            RoutePlanningProfile planningProfile,
            ICollection<ReadyReinforcement> readyReinforcements,
            ICollection<ReinforcementTransit> existingTransits)
        {
            if (threatSources == null)
            {
                throw new ArgumentNullException(nameof(threatSources));
            }

            if (planningProfile == null)
            {
                throw new ArgumentNullException(nameof(planningProfile));
            }

            return DispatchInternal(
                reinforcement,
                completedBuildings,
                territory,
                supplySnapshot,
                routePlanner,
                threatSources,
                planningProfile,
                readyReinforcements,
                existingTransits);
        }

        private static ProductionReinforcementIntegrationResult DispatchInternal(
            ReadyReinforcement reinforcement,
            IEnumerable<BuildingState> completedBuildings,
            TerritoryGraph territory,
            SupplyNetworkSnapshot supplySnapshot,
            ReinforcementRoutePlanner routePlanner,
            IEnumerable<RouteThreatSource>? threatSources,
            RoutePlanningProfile? planningProfile,
            ICollection<ReadyReinforcement> readyReinforcements,
            ICollection<ReinforcementTransit> existingTransits)
        {
            if (reinforcement == null)
            {
                throw new ArgumentNullException(nameof(reinforcement));
            }

            if (completedBuildings == null)
            {
                throw new ArgumentNullException(nameof(completedBuildings));
            }

            if (territory == null)
            {
                throw new ArgumentNullException(nameof(territory));
            }

            if (supplySnapshot == null)
            {
                throw new ArgumentNullException(nameof(supplySnapshot));
            }

            if (routePlanner == null)
            {
                throw new ArgumentNullException(nameof(routePlanner));
            }

            if (readyReinforcements == null)
            {
                throw new ArgumentNullException(nameof(readyReinforcements));
            }

            if (existingTransits == null)
            {
                throw new ArgumentNullException(nameof(existingTransits));
            }

            var ready = FindReady(reinforcement.ReinforcementId, readyReinforcements);
            if (ready == null)
            {
                return ProductionReinforcementIntegrationResult.Failed(
                    ProductionReinforcementIntegrationFailureReason.ReadyReinforcementNotFound);
            }

            var building = FindBuilding(ready.SourceBuildingId, completedBuildings);
            if (building == null)
            {
                return ProductionReinforcementIntegrationResult.Failed(
                    ProductionReinforcementIntegrationFailureReason.SourceBuildingMissing);
            }

            if (!string.Equals(building.OwnerId, ready.FactionId, StringComparison.Ordinal))
            {
                return ProductionReinforcementIntegrationResult.Failed(
                    ProductionReinforcementIntegrationFailureReason.SourceOwnershipConflict);
            }

            if (!string.Equals(building.SectorId, ready.SourceSectorId, StringComparison.Ordinal))
            {
                return ProductionReinforcementIntegrationResult.Failed(
                    ProductionReinforcementIntegrationFailureReason.SourceSectorChanged);
            }

            if (!RoadSegment.PointsEqual(building.Position, ready.SourcePosition))
            {
                return ProductionReinforcementIntegrationResult.Failed(
                    ProductionReinforcementIntegrationFailureReason.SourcePositionChanged);
            }

            var request = new ReinforcementDispatchRequest(
                ready.ReinforcementId,
                ready.FactionId,
                ready.SourceSectorId,
                ready.SourcePosition,
                ready.DestinationSectorId,
                ready.DestinationPosition,
                ready.BaseMovementSpeed);
            var dispatch = threatSources == null
                ? ReinforcementDispatchService.Dispatch(
                    request,
                    territory,
                    supplySnapshot,
                    routePlanner,
                    existingTransits)
                : ReinforcementDispatchService.Dispatch(
                    request,
                    territory,
                    supplySnapshot,
                    routePlanner,
                    threatSources,
                    planningProfile!,
                    existingTransits);
            if (!dispatch.Success)
            {
                return ProductionReinforcementIntegrationResult.Failed(
                    ProductionReinforcementIntegrationFailureReason.DispatchRejected,
                    dispatch.FailureReason);
            }

            if (!readyReinforcements.Remove(ready))
            {
                existingTransits.Remove(dispatch.Transit!);
                return ProductionReinforcementIntegrationResult.Failed(
                    ProductionReinforcementIntegrationFailureReason.ReadyReinforcementNotFound);
            }

            return ProductionReinforcementIntegrationResult.Succeeded(dispatch.Transit!);
        }

        private static ReadyReinforcement? FindReady(
            string reinforcementId,
            IEnumerable<ReadyReinforcement> readyReinforcements)
        {
            ReadyReinforcement? match = null;
            foreach (var reinforcement in readyReinforcements)
            {
                if (reinforcement == null)
                {
                    throw new ArgumentException(
                        "Ready reinforcements cannot contain null.",
                        nameof(readyReinforcements));
                }

                if (!string.Equals(
                    reinforcement.ReinforcementId,
                    reinforcementId,
                    StringComparison.Ordinal))
                {
                    continue;
                }

                if (match != null)
                {
                    throw new ArgumentException(
                        $"Ready reinforcement id '{reinforcementId}' is duplicated.",
                        nameof(readyReinforcements));
                }

                match = reinforcement;
            }

            return match;
        }

        private static BuildingState? FindBuilding(
            string buildingId,
            IEnumerable<BuildingState> completedBuildings)
        {
            BuildingState? match = null;
            foreach (var building in completedBuildings)
            {
                if (building == null)
                {
                    throw new ArgumentException(
                        "Completed buildings cannot contain null.",
                        nameof(completedBuildings));
                }

                if (!string.Equals(building.BuildingId, buildingId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (match != null)
                {
                    throw new ArgumentException(
                        $"Building id '{buildingId}' is duplicated.",
                        nameof(completedBuildings));
                }

                match = building;
            }

            return match;
        }
    }
}
