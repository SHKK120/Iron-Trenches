using System;

namespace IronTrenches.Core
{
    public sealed class SectorState
    {
        public SectorState(string sectorId, string ownerId)
        {
            SectorId = RequireId(sectorId, nameof(sectorId));
            OwnerId = RequireId(ownerId, nameof(ownerId));
        }

        public string SectorId { get; }

        public string OwnerId { get; private set; }

        public void TransferOwnershipTo(string newOwnerId)
        {
            OwnerId = RequireId(newOwnerId, nameof(newOwnerId));
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
