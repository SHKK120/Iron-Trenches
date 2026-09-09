using System;
using System.Collections.Generic;

namespace IronTrenches.Core
{
    public sealed class SupplyNetworkSnapshot
    {
        private readonly Dictionary<string, SectorSupplyStatus> statusBySectorId;

        internal SupplyNetworkSnapshot(
            string factionId,
            IEnumerable<string> allSectorIds,
            IEnumerable<string> activeSupplySourceIds,
            IEnumerable<string> suppliedSectorIds,
            IEnumerable<string> cutOffSectorIds)
        {
            FactionId = RequireId(factionId, nameof(factionId));
            statusBySectorId = new Dictionary<string, SectorSupplyStatus>(StringComparer.Ordinal);

            foreach (var sectorId in allSectorIds)
            {
                statusBySectorId.Add(sectorId, SectorSupplyStatus.NotOwned);
            }

            var supplied = ApplyStatus(
                suppliedSectorIds,
                SectorSupplyStatus.Supplied,
                nameof(suppliedSectorIds));
            var cutOff = ApplyStatus(
                cutOffSectorIds,
                SectorSupplyStatus.CutOff,
                nameof(cutOffSectorIds));

            ActiveSupplySourceIds = CopySortedUnique(
                activeSupplySourceIds,
                nameof(activeSupplySourceIds));
            SuppliedSectorIds = supplied;
            CutOffSectorIds = cutOff;
        }

        public string FactionId { get; }

        public IReadOnlyCollection<string> ActiveSupplySourceIds { get; }

        public IReadOnlyCollection<string> SuppliedSectorIds { get; }

        public IReadOnlyCollection<string> CutOffSectorIds { get; }

        public SectorSupplyStatus GetStatus(string sectorId)
        {
            sectorId = RequireId(sectorId, nameof(sectorId));
            SectorSupplyStatus status;
            if (!statusBySectorId.TryGetValue(sectorId, out status))
            {
                throw new KeyNotFoundException($"Sector '{sectorId}' is not part of this supply snapshot.");
            }

            return status;
        }

        private IReadOnlyCollection<string> ApplyStatus(
            IEnumerable<string> sectorIds,
            SectorSupplyStatus status,
            string parameterName)
        {
            var copy = CopySortedUnique(sectorIds, parameterName);
            foreach (var sectorId in copy)
            {
                if (!statusBySectorId.ContainsKey(sectorId))
                {
                    throw new ArgumentException(
                        $"Sector '{sectorId}' is not part of this supply snapshot.",
                        parameterName);
                }

                if (statusBySectorId[sectorId] != SectorSupplyStatus.NotOwned)
                {
                    throw new ArgumentException(
                        $"Sector '{sectorId}' has more than one supply status.",
                        parameterName);
                }

                statusBySectorId[sectorId] = status;
            }

            return copy;
        }

        private static IReadOnlyCollection<string> CopySortedUnique(
            IEnumerable<string> ids,
            string parameterName)
        {
            if (ids == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            var unique = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in ids)
            {
                if (!unique.Add(RequireId(id, parameterName)))
                {
                    throw new ArgumentException($"Id '{id}' is duplicated.", parameterName);
                }
            }

            var copy = new List<string>(unique);
            copy.Sort(StringComparer.Ordinal);
            return copy.AsReadOnly();
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
