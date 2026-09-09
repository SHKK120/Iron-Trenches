using System;
using System.Collections.Generic;

namespace IronTrenches.Core
{
    public static class BuildingPlacementService
    {
        public static BuildingPlacementResult Place(
            BuildingPlacementRequest request,
            BuildingDefinition definition,
            EconomyState economy,
            TerritoryGraph territory,
            ISectorPlacementAreaResolver areaResolver,
            ICollection<ConstructionSite> existingSites)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (economy == null)
            {
                throw new ArgumentNullException(nameof(economy));
            }

            if (territory == null)
            {
                throw new ArgumentNullException(nameof(territory));
            }

            if (areaResolver == null)
            {
                throw new ArgumentNullException(nameof(areaResolver));
            }

            if (existingSites == null)
            {
                throw new ArgumentNullException(nameof(existingSites));
            }

            var balance = economy.Balance;
            if (!string.Equals(request.BuildingTypeId, definition.BuildingTypeId, StringComparison.Ordinal)
                || !string.Equals(request.BuilderFactionId, economy.FactionId, StringComparison.Ordinal)
                || ContainsSiteId(existingSites, request.SiteId))
            {
                return BuildingPlacementResult.Failed(
                    BuildingPlacementFailureReason.InvalidRequest,
                    balance);
            }

            if (!territory.Topology.ContainsSector(request.TargetSectorId))
            {
                return BuildingPlacementResult.Failed(
                    BuildingPlacementFailureReason.UnknownSector,
                    balance);
            }

            if (!string.Equals(
                territory.GetSector(request.TargetSectorId).OwnerId,
                request.BuilderFactionId,
                StringComparison.Ordinal))
            {
                return BuildingPlacementResult.Failed(
                    BuildingPlacementFailureReason.EnemyTerritory,
                    balance);
            }

            if (!IsFinite(request.Position))
            {
                return BuildingPlacementResult.Failed(
                    BuildingPlacementFailureReason.InvalidPosition,
                    balance);
            }

            var resolvedSectorId = areaResolver.ResolveSector(request.Position);
            if (!string.Equals(resolvedSectorId, request.TargetSectorId, StringComparison.Ordinal))
            {
                return BuildingPlacementResult.Failed(
                    BuildingPlacementFailureReason.OutsideTargetSector,
                    balance);
            }

            if (!areaResolver.ContainsFootprint(
                request.TargetSectorId,
                request.Position,
                definition.FootprintRadius))
            {
                return BuildingPlacementResult.Failed(
                    BuildingPlacementFailureReason.FootprintCrossesSectorBoundary,
                    balance);
            }

            if (OverlapsExistingSite(request.Position, definition.FootprintRadius, existingSites))
            {
                return BuildingPlacementResult.Failed(
                    BuildingPlacementFailureReason.Occupied,
                    balance);
            }

            if (!economy.CanAfford(definition.Cost))
            {
                return BuildingPlacementResult.Failed(
                    BuildingPlacementFailureReason.InsufficientFunds,
                    balance);
            }

            var site = new ConstructionSite(
                request.SiteId,
                definition.BuildingTypeId,
                request.BuilderFactionId,
                request.TargetSectorId,
                request.Position,
                definition.FootprintRadius);

            existingSites.Add(site);
            try
            {
                if (!economy.Spend(definition.Cost))
                {
                    existingSites.Remove(site);
                    return BuildingPlacementResult.Failed(
                        BuildingPlacementFailureReason.InsufficientFunds,
                        economy.Balance);
                }
            }
            catch
            {
                existingSites.Remove(site);
                throw;
            }

            return BuildingPlacementResult.Succeeded(
                site,
                definition.Cost,
                balance,
                economy.Balance);
        }

        private static bool ContainsSiteId(
            IEnumerable<ConstructionSite> existingSites,
            string siteId)
        {
            foreach (var site in existingSites)
            {
                if (site == null)
                {
                    throw new ArgumentException(
                        "Existing construction sites cannot contain null.",
                        nameof(existingSites));
                }

                if (string.Equals(site.SiteId, siteId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool OverlapsExistingSite(
            WorldPoint position,
            float footprintRadius,
            IEnumerable<ConstructionSite> existingSites)
        {
            foreach (var site in existingSites)
            {
                var deltaX = (double)position.X - site.Position.X;
                var deltaZ = (double)position.Z - site.Position.Z;
                var combinedRadius = (double)footprintRadius + site.FootprintRadius;
                if ((deltaX * deltaX) + (deltaZ * deltaZ) < combinedRadius * combinedRadius)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsFinite(WorldPoint point)
        {
            return !float.IsNaN(point.X)
                && !float.IsInfinity(point.X)
                && !float.IsNaN(point.Z)
                && !float.IsInfinity(point.Z);
        }
    }
}
