using System;
using System.Collections.Generic;

namespace IronTrenches.Core
{
    public static class ProductionService
    {
        public static ProductionEnqueueResult Enqueue(
            ProductionRequest request,
            TerritoryGraph territory,
            EconomyState economy,
            IEnumerable<BuildingState> completedBuildings,
            IEnumerable<ConstructionSite> constructionSites,
            IEnumerable<ProductionDefinition> definitions,
            IEnumerable<ProductionFacilityProfile> facilityProfiles,
            ICollection<ProductionQueue> productionQueues,
            IEnumerable<ReadyReinforcement> readyReinforcements)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (territory == null)
            {
                throw new ArgumentNullException(nameof(territory));
            }

            if (economy == null)
            {
                throw new ArgumentNullException(nameof(economy));
            }

            if (productionQueues == null)
            {
                throw new ArgumentNullException(nameof(productionQueues));
            }

            var buildings = MaterializeBuildings(completedBuildings);
            var sites = MaterializeSites(constructionSites);
            var definitionsByUnitType = MaterializeDefinitions(definitions);
            var profilesByBuildingType = MaterializeProfiles(facilityProfiles);
            var ready = MaterializeReady(readyReinforcements);
            ValidateQueues(productionQueues);

            var balance = economy.Balance;
            if (!string.Equals(request.RequestedByFactionId, economy.FactionId, StringComparison.Ordinal))
            {
                return ProductionEnqueueResult.Failed(
                    ProductionEnqueueFailureReason.FactionMismatch,
                    balance);
            }

            BuildingState? facility = null;
            foreach (var building in buildings)
            {
                if (string.Equals(
                    building.BuildingId,
                    request.FacilityBuildingId,
                    StringComparison.Ordinal))
                {
                    facility = building;
                    break;
                }
            }

            if (facility == null)
            {
                foreach (var site in sites)
                {
                    if (string.Equals(site.SiteId, request.FacilityBuildingId, StringComparison.Ordinal))
                    {
                        return ProductionEnqueueResult.Failed(
                            ProductionEnqueueFailureReason.FacilityIncomplete,
                            balance);
                    }
                }

                return ProductionEnqueueResult.Failed(
                    ProductionEnqueueFailureReason.UnknownFacility,
                    balance);
            }

            if (!string.Equals(facility.OwnerId, request.RequestedByFactionId, StringComparison.Ordinal))
            {
                return ProductionEnqueueResult.Failed(
                    ProductionEnqueueFailureReason.EnemyFacility,
                    balance);
            }

            if (!profilesByBuildingType.TryGetValue(facility.BuildingTypeId, out var profile))
            {
                return ProductionEnqueueResult.Failed(
                    ProductionEnqueueFailureReason.UnknownFacilityProfile,
                    balance);
            }

            if (!profile.CanProduce(request.UnitTypeId))
            {
                return ProductionEnqueueResult.Failed(
                    ProductionEnqueueFailureReason.UnsupportedUnitType,
                    balance);
            }

            if (!definitionsByUnitType.TryGetValue(request.UnitTypeId, out var definition))
            {
                return ProductionEnqueueResult.Failed(
                    ProductionEnqueueFailureReason.UnknownUnitType,
                    balance);
            }

            foreach (var queue in productionQueues)
            {
                if (queue.ContainsOrderId(request.OrderId))
                {
                    return ProductionEnqueueResult.Failed(
                        ProductionEnqueueFailureReason.DuplicateOrderId,
                        balance);
                }

                if (queue.ContainsReinforcementId(request.ReinforcementId))
                {
                    return ProductionEnqueueResult.Failed(
                        ProductionEnqueueFailureReason.DuplicateReinforcementId,
                        balance);
                }
            }

            foreach (var reinforcement in ready)
            {
                if (string.Equals(
                    reinforcement.ReinforcementId,
                    request.ReinforcementId,
                    StringComparison.Ordinal))
                {
                    return ProductionEnqueueResult.Failed(
                        ProductionEnqueueFailureReason.DuplicateReinforcementId,
                        balance);
                }
            }

            if (!territory.Topology.ContainsSector(request.DestinationSectorId))
            {
                return ProductionEnqueueResult.Failed(
                    ProductionEnqueueFailureReason.UnknownDestinationSector,
                    balance);
            }

            if (!RoadSegment.IsFinite(request.DestinationPosition))
            {
                return ProductionEnqueueResult.Failed(
                    ProductionEnqueueFailureReason.InvalidDestination,
                    balance);
            }

            if (!economy.CanAfford(definition.PrototypeCost))
            {
                return ProductionEnqueueResult.Failed(
                    ProductionEnqueueFailureReason.InsufficientFunds,
                    balance);
            }

            ProductionQueue? targetQueue = null;
            foreach (var queue in productionQueues)
            {
                if (string.Equals(
                    queue.FacilityBuildingId,
                    facility.BuildingId,
                    StringComparison.Ordinal))
                {
                    targetQueue = queue;
                    break;
                }
            }

            var createdQueue = false;
            if (targetQueue == null)
            {
                targetQueue = new ProductionQueue(facility.BuildingId);
                productionQueues.Add(targetQueue);
                createdQueue = true;
            }

            var orderToAdd = new ProductionOrder(request, definition);
            targetQueue.Enqueue(orderToAdd);
            try
            {
                if (!economy.Spend(definition.PrototypeCost))
                {
                    targetQueue.Remove(orderToAdd);
                    if (createdQueue)
                    {
                        productionQueues.Remove(targetQueue);
                    }

                    return ProductionEnqueueResult.Failed(
                        ProductionEnqueueFailureReason.InsufficientFunds,
                        economy.Balance);
                }
            }
            catch
            {
                targetQueue.Remove(orderToAdd);
                if (createdQueue)
                {
                    productionQueues.Remove(targetQueue);
                }

                throw;
            }

            return ProductionEnqueueResult.Succeeded(
                orderToAdd,
                balance,
                economy.Balance);
        }

        private static List<BuildingState> MaterializeBuildings(
            IEnumerable<BuildingState> buildings)
        {
            if (buildings == null)
            {
                throw new ArgumentNullException(nameof(buildings));
            }

            var copy = new List<BuildingState>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var building in buildings)
            {
                if (building == null)
                {
                    throw new ArgumentException("Completed buildings cannot contain null.", nameof(buildings));
                }

                if (!ids.Add(building.BuildingId))
                {
                    throw new ArgumentException($"Building id '{building.BuildingId}' is duplicated.", nameof(buildings));
                }

                copy.Add(building);
            }

            return copy;
        }

        private static List<ConstructionSite> MaterializeSites(
            IEnumerable<ConstructionSite> sites)
        {
            if (sites == null)
            {
                throw new ArgumentNullException(nameof(sites));
            }

            var copy = new List<ConstructionSite>();
            foreach (var site in sites)
            {
                if (site == null)
                {
                    throw new ArgumentException("Construction sites cannot contain null.", nameof(sites));
                }

                copy.Add(site);
            }

            return copy;
        }

        private static Dictionary<string, ProductionDefinition> MaterializeDefinitions(
            IEnumerable<ProductionDefinition> definitions)
        {
            if (definitions == null)
            {
                throw new ArgumentNullException(nameof(definitions));
            }

            var byId = new Dictionary<string, ProductionDefinition>(StringComparer.Ordinal);
            foreach (var definition in definitions)
            {
                if (definition == null)
                {
                    throw new ArgumentException("Production definitions cannot contain null.", nameof(definitions));
                }

                if (byId.ContainsKey(definition.UnitTypeId))
                {
                    throw new ArgumentException($"Unit type id '{definition.UnitTypeId}' is duplicated.", nameof(definitions));
                }

                byId.Add(definition.UnitTypeId, definition);
            }

            return byId;
        }

        private static Dictionary<string, ProductionFacilityProfile> MaterializeProfiles(
            IEnumerable<ProductionFacilityProfile> profiles)
        {
            if (profiles == null)
            {
                throw new ArgumentNullException(nameof(profiles));
            }

            var byId = new Dictionary<string, ProductionFacilityProfile>(StringComparer.Ordinal);
            foreach (var profile in profiles)
            {
                if (profile == null)
                {
                    throw new ArgumentException("Facility profiles cannot contain null.", nameof(profiles));
                }

                if (byId.ContainsKey(profile.BuildingTypeId))
                {
                    throw new ArgumentException($"Building type id '{profile.BuildingTypeId}' is duplicated.", nameof(profiles));
                }

                byId.Add(profile.BuildingTypeId, profile);
            }

            return byId;
        }

        private static List<ReadyReinforcement> MaterializeReady(
            IEnumerable<ReadyReinforcement> readyReinforcements)
        {
            if (readyReinforcements == null)
            {
                throw new ArgumentNullException(nameof(readyReinforcements));
            }

            var copy = new List<ReadyReinforcement>();
            foreach (var reinforcement in readyReinforcements)
            {
                if (reinforcement == null)
                {
                    throw new ArgumentException("Ready reinforcements cannot contain null.", nameof(readyReinforcements));
                }

                copy.Add(reinforcement);
            }

            return copy;
        }

        private static void ValidateQueues(IEnumerable<ProductionQueue> queues)
        {
            var facilityIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var queue in queues)
            {
                if (queue == null)
                {
                    throw new ArgumentException("Production queues cannot contain null.", nameof(queues));
                }

                if (!facilityIds.Add(queue.FacilityBuildingId))
                {
                    throw new ArgumentException(
                        $"Facility queue '{queue.FacilityBuildingId}' is duplicated.",
                        nameof(queues));
                }
            }
        }
    }
}
