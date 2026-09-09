using System;

namespace IronTrenches.Core
{
    public sealed class SupplySourceDefinition
    {
        public SupplySourceDefinition(string sourceId, string factionId, string sectorId)
        {
            SourceId = RequireId(sourceId, nameof(sourceId));
            FactionId = RequireId(factionId, nameof(factionId));
            SectorId = RequireId(sectorId, nameof(sectorId));
        }

        public string SourceId { get; }

        public string FactionId { get; }

        public string SectorId { get; }

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
