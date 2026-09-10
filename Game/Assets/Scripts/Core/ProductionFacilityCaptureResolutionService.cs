using System;
using System.Collections.Generic;

namespace IronTrenches.Core
{
    public static class ProductionFacilityCaptureResolutionService
    {
        public static ProductionFacilityCaptureResolution Resolve(
            SectorBuildingTransferResult captureResult,
            IEnumerable<BuildingState> completedBuildings,
            IEnumerable<ProductionFacilityProfile> facilityProfiles,
            ICollection<ProductionQueue> productionQueues,
            ProductionCaptureQueuePolicy policy)
        {
            if (captureResult == null)
            {
                throw new ArgumentNullException(nameof(captureResult));
            }

            if (completedBuildings == null)
            {
                throw new ArgumentNullException(nameof(completedBuildings));
            }

            if (facilityProfiles == null)
            {
                throw new ArgumentNullException(nameof(facilityProfiles));
            }

            if (productionQueues == null)
            {
                throw new ArgumentNullException(nameof(productionQueues));
            }

            if (!Enum.IsDefined(typeof(ProductionCaptureQueuePolicy), policy))
            {
                return Failed(
                    captureResult,
                    ProductionFacilityCaptureResolutionFailureReason.UnsupportedPolicy);
            }

            if (!captureResult.CaptureChanged)
            {
                return Succeeded(captureResult, Array.Empty<CapturedFacilityQueueResolution>());
            }

            if (string.Equals(
                captureResult.PreviousOwnerId,
                captureResult.NewOwnerId,
                StringComparison.Ordinal))
            {
                return Failed(
                    captureResult,
                    ProductionFacilityCaptureResolutionFailureReason.InconsistentCaptureResult);
            }

            var buildings = MaterializeBuildings(completedBuildings);
            var productionBuildingTypeIds = MaterializeProfiles(facilityProfiles);
            var queues = MaterializeQueues(productionQueues);
            var transferredIds = new HashSet<string>(StringComparer.Ordinal);
            var targets = new List<BuildingState>();

            foreach (var buildingId in captureResult.TransferredBuildingIds)
            {
                if (!transferredIds.Add(buildingId)
                    || !buildings.TryGetValue(buildingId, out var building)
                    || !string.Equals(building.SectorId, captureResult.SectorId, StringComparison.Ordinal)
                    || !string.Equals(building.OwnerId, captureResult.NewOwnerId, StringComparison.Ordinal))
                {
                    return Failed(
                        captureResult,
                        ProductionFacilityCaptureResolutionFailureReason.InconsistentCaptureResult);
                }

                targets.Add(building);
            }

            foreach (var target in targets)
            {
                var isProductionFacility = productionBuildingTypeIds.Contains(target.BuildingTypeId);
                if (!queues.TryGetValue(target.BuildingId, out var queue))
                {
                    continue;
                }

                if (!isProductionFacility)
                {
                    return Failed(
                        captureResult,
                        ProductionFacilityCaptureResolutionFailureReason.InconsistentQueueLinkage);
                }

                foreach (var order in queue.Orders)
                {
                    if (!string.Equals(
                        order.RequestedByFactionId,
                        captureResult.PreviousOwnerId,
                        StringComparison.Ordinal))
                    {
                        return Failed(
                            captureResult,
                            ProductionFacilityCaptureResolutionFailureReason.InconsistentQueueLinkage);
                    }
                }
            }

            var facilityResolutions = new List<CapturedFacilityQueueResolution>();
            foreach (var target in targets)
            {
                var isProductionFacility = productionBuildingTypeIds.Contains(target.BuildingTypeId);
                var cancelledIds = new List<string>();
                if (isProductionFacility && queues.TryGetValue(target.BuildingId, out var queue))
                {
                    foreach (var order in queue.CancelAll())
                    {
                        cancelledIds.Add(order.OrderId);
                    }
                }

                facilityResolutions.Add(new CapturedFacilityQueueResolution(
                    target.BuildingId,
                    captureResult.PreviousOwnerId,
                    captureResult.NewOwnerId,
                    isProductionFacility,
                    isProductionFacility,
                    cancelledIds));
            }

            return Succeeded(captureResult, facilityResolutions);
        }

        private static Dictionary<string, BuildingState> MaterializeBuildings(
            IEnumerable<BuildingState> buildings)
        {
            var byId = new Dictionary<string, BuildingState>(StringComparer.Ordinal);
            foreach (var building in buildings)
            {
                if (building == null)
                {
                    throw new ArgumentException(
                        "Completed buildings cannot contain null.",
                        nameof(buildings));
                }

                if (byId.ContainsKey(building.BuildingId))
                {
                    throw new ArgumentException(
                        $"Building id '{building.BuildingId}' is duplicated.",
                        nameof(buildings));
                }

                byId.Add(building.BuildingId, building);
            }

            return byId;
        }

        private static HashSet<string> MaterializeProfiles(
            IEnumerable<ProductionFacilityProfile> profiles)
        {
            var buildingTypeIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var profile in profiles)
            {
                if (profile == null)
                {
                    throw new ArgumentException(
                        "Facility profiles cannot contain null.",
                        nameof(profiles));
                }

                if (!buildingTypeIds.Add(profile.BuildingTypeId))
                {
                    throw new ArgumentException(
                        $"Building type id '{profile.BuildingTypeId}' is duplicated.",
                        nameof(profiles));
                }
            }

            return buildingTypeIds;
        }

        private static Dictionary<string, ProductionQueue> MaterializeQueues(
            IEnumerable<ProductionQueue> queues)
        {
            var byFacilityId = new Dictionary<string, ProductionQueue>(StringComparer.Ordinal);
            foreach (var queue in queues)
            {
                if (queue == null)
                {
                    throw new ArgumentException(
                        "Production queues cannot contain null.",
                        nameof(queues));
                }

                if (byFacilityId.ContainsKey(queue.FacilityBuildingId))
                {
                    throw new ArgumentException(
                        $"Facility queue '{queue.FacilityBuildingId}' is duplicated.",
                        nameof(queues));
                }

                byFacilityId.Add(queue.FacilityBuildingId, queue);
            }

            return byFacilityId;
        }

        private static ProductionFacilityCaptureResolution Succeeded(
            SectorBuildingTransferResult captureResult,
            IEnumerable<CapturedFacilityQueueResolution> facilities)
        {
            return new ProductionFacilityCaptureResolution(
                captureResult.SectorId,
                captureResult.PreviousOwnerId,
                captureResult.NewOwnerId,
                captureResult.CaptureChanged,
                ProductionFacilityCaptureResolutionFailureReason.None,
                facilities);
        }

        private static ProductionFacilityCaptureResolution Failed(
            SectorBuildingTransferResult captureResult,
            ProductionFacilityCaptureResolutionFailureReason failureReason)
        {
            return new ProductionFacilityCaptureResolution(
                captureResult.SectorId,
                captureResult.PreviousOwnerId,
                captureResult.NewOwnerId,
                captureResult.CaptureChanged,
                failureReason,
                Array.Empty<CapturedFacilityQueueResolution>());
        }
    }
}
