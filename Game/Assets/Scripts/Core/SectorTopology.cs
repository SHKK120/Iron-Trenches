using System;
using System.Collections.Generic;

namespace IronTrenches.Core
{
    public sealed class SectorTopology
    {
        private readonly Dictionary<string, HashSet<string>> neighborsBySector =
            new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        public IReadOnlyCollection<string> SectorIds
        {
            get
            {
                var sectorIds = new List<string>(neighborsBySector.Keys);
                sectorIds.Sort(StringComparer.Ordinal);
                return sectorIds.AsReadOnly();
            }
        }

        public void RegisterSector(string sectorId)
        {
            sectorId = RequireId(sectorId, nameof(sectorId));
            if (!neighborsBySector.ContainsKey(sectorId))
            {
                neighborsBySector.Add(
                    sectorId,
                    new HashSet<string>(StringComparer.Ordinal));
            }
        }

        public bool ContainsSector(string sectorId)
        {
            sectorId = RequireId(sectorId, nameof(sectorId));
            return neighborsBySector.ContainsKey(sectorId);
        }

        public void AddBidirectionalAdjacency(string firstSectorId, string secondSectorId)
        {
            firstSectorId = RequireId(firstSectorId, nameof(firstSectorId));
            secondSectorId = RequireId(secondSectorId, nameof(secondSectorId));

            if (string.Equals(firstSectorId, secondSectorId, StringComparison.Ordinal))
            {
                throw new ArgumentException("A sector cannot be adjacent to itself.", nameof(secondSectorId));
            }

            var firstNeighbors = GetRegisteredNeighbors(firstSectorId);
            var secondNeighbors = GetRegisteredNeighbors(secondSectorId);
            firstNeighbors.Add(secondSectorId);
            secondNeighbors.Add(firstSectorId);
        }

        public bool AreAdjacent(string firstSectorId, string secondSectorId)
        {
            firstSectorId = RequireId(firstSectorId, nameof(firstSectorId));
            secondSectorId = RequireId(secondSectorId, nameof(secondSectorId));

            HashSet<string>? neighbors;
            return neighborsBySector.TryGetValue(firstSectorId, out neighbors)
                && neighbors.Contains(secondSectorId);
        }

        public IReadOnlyCollection<string> GetNeighbors(string sectorId)
        {
            sectorId = RequireId(sectorId, nameof(sectorId));
            var neighbors = new List<string>(GetRegisteredNeighbors(sectorId));
            neighbors.Sort(StringComparer.Ordinal);
            return neighbors.AsReadOnly();
        }

        private HashSet<string> GetRegisteredNeighbors(string sectorId)
        {
            HashSet<string>? neighbors;
            if (!neighborsBySector.TryGetValue(sectorId, out neighbors))
            {
                throw new KeyNotFoundException($"Sector '{sectorId}' is not registered.");
            }

            return neighbors;
        }

        private static string RequireId(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("An id cannot be empty.", parameterName);
            }

            return value;
        }
    }
}
