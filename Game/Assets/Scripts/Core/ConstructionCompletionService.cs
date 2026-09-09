using System;
using System.Collections.Generic;

namespace IronTrenches.Core
{
    public static class ConstructionCompletionService
    {
        public static ConstructionCompletionResult Complete(
            string siteId,
            string buildingId,
            ICollection<ConstructionSite> constructionSites,
            ICollection<BuildingState> completedBuildings)
        {
            if (constructionSites == null)
            {
                throw new ArgumentNullException(nameof(constructionSites));
            }

            if (completedBuildings == null)
            {
                throw new ArgumentNullException(nameof(completedBuildings));
            }

            ValidateConstructionSites(constructionSites);
            ValidateCompletedBuildings(completedBuildings);

            if (string.IsNullOrWhiteSpace(siteId))
            {
                return ConstructionCompletionResult.Failed(
                    ConstructionCompletionFailureReason.InvalidSiteId);
            }

            if (string.IsNullOrWhiteSpace(buildingId))
            {
                return ConstructionCompletionResult.Failed(
                    ConstructionCompletionFailureReason.InvalidBuildingId);
            }

            ConstructionSite? site = null;
            foreach (var candidate in constructionSites)
            {
                if (string.Equals(candidate.SiteId, siteId, StringComparison.Ordinal))
                {
                    site = candidate;
                    break;
                }
            }

            if (site == null)
            {
                return ConstructionCompletionResult.Failed(
                    ConstructionCompletionFailureReason.UnknownSite);
            }

            foreach (var building in completedBuildings)
            {
                if (string.Equals(building.BuildingId, buildingId, StringComparison.Ordinal))
                {
                    return ConstructionCompletionResult.Failed(
                        ConstructionCompletionFailureReason.DuplicateBuildingId);
                }
            }

            var completedBuilding = new BuildingState(
                buildingId,
                site.BuildingTypeId,
                site.OwnerId,
                site.SectorId,
                site.Position,
                site.FootprintRadius);

            completedBuildings.Add(completedBuilding);
            try
            {
                if (!constructionSites.Remove(site))
                {
                    completedBuildings.Remove(completedBuilding);
                    return ConstructionCompletionResult.Failed(
                        ConstructionCompletionFailureReason.UnknownSite);
                }
            }
            catch
            {
                completedBuildings.Remove(completedBuilding);
                throw;
            }

            return ConstructionCompletionResult.Succeeded(site.SiteId, completedBuilding);
        }

        private static void ValidateConstructionSites(
            IEnumerable<ConstructionSite> constructionSites)
        {
            var siteIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var site in constructionSites)
            {
                if (site == null)
                {
                    throw new ArgumentException(
                        "Construction sites cannot contain null.",
                        nameof(constructionSites));
                }

                if (!siteIds.Add(site.SiteId))
                {
                    throw new ArgumentException(
                        $"Construction site id '{site.SiteId}' is duplicated.",
                        nameof(constructionSites));
                }
            }
        }

        private static void ValidateCompletedBuildings(
            IEnumerable<BuildingState> completedBuildings)
        {
            var buildingIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var building in completedBuildings)
            {
                if (building == null)
                {
                    throw new ArgumentException(
                        "Completed buildings cannot contain null.",
                        nameof(completedBuildings));
                }

                if (!buildingIds.Add(building.BuildingId))
                {
                    throw new ArgumentException(
                        $"Completed building id '{building.BuildingId}' is duplicated.",
                        nameof(completedBuildings));
                }
            }
        }
    }
}
