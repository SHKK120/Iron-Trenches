using System;
using System.Collections.Generic;

namespace IronTrenches.Core
{
    public static class FrontlineResolver
    {
        public static IReadOnlyCollection<FrontlineEdge> Resolve(
            SectorTopology topology,
            IEnumerable<SectorState> sectors)
        {
            if (topology == null)
            {
                throw new ArgumentNullException(nameof(topology));
            }

            if (sectors == null)
            {
                throw new ArgumentNullException(nameof(sectors));
            }

            var stateBySector = IndexSectorStates(topology, sectors);
            var frontlines = new List<FrontlineEdge>();

            foreach (var sectorId in topology.SectorIds)
            {
                foreach (var neighborId in topology.GetNeighbors(sectorId))
                {
                    if (string.CompareOrdinal(sectorId, neighborId) >= 0)
                    {
                        continue;
                    }

                    if (!string.Equals(
                        stateBySector[sectorId].OwnerId,
                        stateBySector[neighborId].OwnerId,
                        StringComparison.Ordinal))
                    {
                        frontlines.Add(new FrontlineEdge(sectorId, neighborId));
                    }
                }
            }

            return frontlines.AsReadOnly();
        }

        private static Dictionary<string, SectorState> IndexSectorStates(
            SectorTopology topology,
            IEnumerable<SectorState> sectors)
        {
            var stateBySector = new Dictionary<string, SectorState>(StringComparer.Ordinal);
            foreach (var sector in sectors)
            {
                if (sector == null)
                {
                    throw new ArgumentException("Sector states cannot contain null.", nameof(sectors));
                }

                if (!topology.ContainsSector(sector.SectorId))
                {
                    throw new ArgumentException(
                        $"Sector '{sector.SectorId}' is not registered in the topology.",
                        nameof(sectors));
                }

                if (!stateBySector.TryAdd(sector.SectorId, sector))
                {
                    throw new ArgumentException(
                        $"Sector '{sector.SectorId}' has more than one state.",
                        nameof(sectors));
                }
            }

            foreach (var sectorId in topology.SectorIds)
            {
                if (!stateBySector.ContainsKey(sectorId))
                {
                    throw new ArgumentException(
                        $"Registered sector '{sectorId}' has no state.",
                        nameof(sectors));
                }
            }

            return stateBySector;
        }
    }
}
