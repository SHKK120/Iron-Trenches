using System;

namespace IronTrenches.Core
{
    public sealed class SectorControlAnchor
    {
        public SectorControlAnchor(string anchorId, string sectorId)
        {
            AnchorId = RequireId(anchorId, nameof(anchorId));
            SectorId = RequireId(sectorId, nameof(sectorId));
        }

        public string AnchorId { get; }

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
