using System;
using System.Collections.Generic;

namespace IronTrenches.Core
{
    public static class SectorIncomeResolver
    {
        public static long Resolve(
            TerritoryGraph territory,
            IEnumerable<SectorIncomeProfile> profiles,
            string factionId)
        {
            if (territory == null)
            {
                throw new ArgumentNullException(nameof(territory));
            }

            if (string.IsNullOrWhiteSpace(factionId))
            {
                throw new ArgumentException("An id cannot be empty.", nameof(factionId));
            }

            var profileBySector = IndexProfiles(territory, profiles);
            long total = 0;
            foreach (var sectorId in territory.Topology.SectorIds)
            {
                if (string.Equals(
                    territory.GetSector(sectorId).OwnerId,
                    factionId,
                    StringComparison.Ordinal))
                {
                    total = checked(total + profileBySector[sectorId].IncomeValue);
                }
            }

            return total;
        }

        private static Dictionary<string, SectorIncomeProfile> IndexProfiles(
            TerritoryGraph territory,
            IEnumerable<SectorIncomeProfile> profiles)
        {
            if (profiles == null)
            {
                throw new ArgumentNullException(nameof(profiles));
            }

            var profileBySector = new Dictionary<string, SectorIncomeProfile>(StringComparer.Ordinal);
            foreach (var profile in profiles)
            {
                if (profile == null)
                {
                    throw new ArgumentException("Income profiles cannot contain null.", nameof(profiles));
                }

                if (!territory.Topology.ContainsSector(profile.SectorId))
                {
                    throw new ArgumentException(
                        $"Income profile targets unregistered sector '{profile.SectorId}'.",
                        nameof(profiles));
                }

                if (!profileBySector.TryAdd(profile.SectorId, profile))
                {
                    throw new ArgumentException(
                        $"Sector '{profile.SectorId}' has more than one income profile.",
                        nameof(profiles));
                }
            }

            foreach (var sectorId in territory.Topology.SectorIds)
            {
                if (!profileBySector.ContainsKey(sectorId))
                {
                    throw new ArgumentException(
                        $"Registered sector '{sectorId}' has no income profile.",
                        nameof(profiles));
                }
            }

            return profileBySector;
        }
    }
}
