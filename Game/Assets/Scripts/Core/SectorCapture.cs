using System;

namespace IronTrenches.Core
{
    public readonly struct CaptureResult
    {
        public CaptureResult(string previousOwnerId, string newOwnerId, bool changed)
        {
            PreviousOwnerId = previousOwnerId;
            NewOwnerId = newOwnerId;
            Changed = changed;
        }

        public string PreviousOwnerId { get; }

        public string NewOwnerId { get; }

        public bool Changed { get; }
    }

    public static class SectorCapture
    {
        public static CaptureResult Complete(SectorState sector, string newOwnerId)
        {
            if (sector == null)
            {
                throw new ArgumentNullException(nameof(sector));
            }

            if (string.IsNullOrWhiteSpace(newOwnerId))
            {
                throw new ArgumentException("An owner id cannot be empty.", nameof(newOwnerId));
            }

            var previousOwnerId = sector.OwnerId;
            var changed = !string.Equals(previousOwnerId, newOwnerId, StringComparison.Ordinal);
            if (changed)
            {
                sector.TransferOwnershipTo(newOwnerId);
            }

            return new CaptureResult(previousOwnerId, newOwnerId, changed);
        }
    }
}
