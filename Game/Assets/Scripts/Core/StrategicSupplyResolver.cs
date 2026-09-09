using System;
using System.Collections.Generic;

namespace IronTrenches.Core
{
    public static class StrategicSupplyResolver
    {
        public static SupplyNetworkSnapshot Resolve(
            TerritoryGraph territory,
            string factionId,
            IEnumerable<SupplySourceDefinition> sources)
        {
            if (territory == null)
            {
                throw new ArgumentNullException(nameof(territory));
            }

            if (string.IsNullOrWhiteSpace(factionId))
            {
                throw new ArgumentException("An id cannot be empty.", nameof(factionId));
            }

            var sourceList = ValidateAndMaterializeSources(territory, sources);
            var ownedSectorIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var sectorId in territory.Topology.SectorIds)
            {
                if (string.Equals(
                    territory.GetSector(sectorId).OwnerId,
                    factionId,
                    StringComparison.Ordinal))
                {
                    ownedSectorIds.Add(sectorId);
                }
            }

            var activeSourceIds = new List<string>();
            var suppliedSectorIds = new HashSet<string>(StringComparer.Ordinal);
            var frontier = new Queue<string>();
            foreach (var source in sourceList)
            {
                if (!string.Equals(source.FactionId, factionId, StringComparison.Ordinal)
                    || !ownedSectorIds.Contains(source.SectorId))
                {
                    continue;
                }

                activeSourceIds.Add(source.SourceId);
                if (suppliedSectorIds.Add(source.SectorId))
                {
                    frontier.Enqueue(source.SectorId);
                }
            }

            while (frontier.Count > 0)
            {
                var sectorId = frontier.Dequeue();
                foreach (var neighborId in territory.Topology.GetNeighbors(sectorId))
                {
                    if (ownedSectorIds.Contains(neighborId)
                        && suppliedSectorIds.Add(neighborId))
                    {
                        frontier.Enqueue(neighborId);
                    }
                }
            }

            var cutOffSectorIds = new List<string>();
            foreach (var sectorId in ownedSectorIds)
            {
                if (!suppliedSectorIds.Contains(sectorId))
                {
                    cutOffSectorIds.Add(sectorId);
                }
            }

            return new SupplyNetworkSnapshot(
                factionId,
                territory.Topology.SectorIds,
                activeSourceIds,
                suppliedSectorIds,
                cutOffSectorIds);
        }

        private static List<SupplySourceDefinition> ValidateAndMaterializeSources(
            TerritoryGraph territory,
            IEnumerable<SupplySourceDefinition> sources)
        {
            if (sources == null)
            {
                throw new ArgumentNullException(nameof(sources));
            }

            var sourceList = new List<SupplySourceDefinition>();
            var sourceIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var source in sources)
            {
                if (source == null)
                {
                    throw new ArgumentException(
                        "Supply sources cannot contain null.",
                        nameof(sources));
                }

                if (!sourceIds.Add(source.SourceId))
                {
                    throw new ArgumentException(
                        $"Supply source id '{source.SourceId}' is duplicated.",
                        nameof(sources));
                }

                if (!territory.Topology.ContainsSector(source.SectorId))
                {
                    throw new ArgumentException(
                        $"Supply source '{source.SourceId}' targets unregistered sector '{source.SectorId}'.",
                        nameof(sources));
                }

                sourceList.Add(source);
            }

            return sourceList;
        }
    }
}
