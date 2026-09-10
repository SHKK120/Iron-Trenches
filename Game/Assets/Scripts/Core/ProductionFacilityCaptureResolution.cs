using System;
using System.Collections.Generic;

namespace IronTrenches.Core
{
    public enum ProductionFacilityCaptureResolutionFailureReason
    {
        None,
        InconsistentCaptureResult,
        InconsistentQueueLinkage,
        UnsupportedPolicy
    }

    public sealed class CapturedFacilityQueueResolution
    {
        internal CapturedFacilityQueueResolution(
            string capturedFacilityId,
            string previousOwnerId,
            string newOwnerId,
            bool wasProductionFacility,
            bool resolutionApplied,
            IEnumerable<string> cancelledOrderIds)
        {
            CapturedFacilityId = RequireId(capturedFacilityId, nameof(capturedFacilityId));
            PreviousOwnerId = RequireId(previousOwnerId, nameof(previousOwnerId));
            NewOwnerId = RequireId(newOwnerId, nameof(newOwnerId));
            WasProductionFacility = wasProductionFacility;
            ResolutionApplied = resolutionApplied;

            if (cancelledOrderIds == null)
            {
                throw new ArgumentNullException(nameof(cancelledOrderIds));
            }

            var ids = new List<string>();
            foreach (var orderId in cancelledOrderIds)
            {
                ids.Add(RequireId(orderId, nameof(cancelledOrderIds)));
            }

            CancelledOrderIds = ids.AsReadOnly();
        }

        public string CapturedFacilityId { get; }

        public string PreviousOwnerId { get; }

        public string NewOwnerId { get; }

        public bool WasProductionFacility { get; }

        public bool ResolutionApplied { get; }

        public IReadOnlyList<string> CancelledOrderIds { get; }

        public int CancelledOrderCount => CancelledOrderIds.Count;

        private static string RequireId(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("An id cannot be empty.", parameterName);
            }

            return value;
        }
    }

    public sealed class ProductionFacilityCaptureResolution
    {
        internal ProductionFacilityCaptureResolution(
            string sectorId,
            string previousOwnerId,
            string newOwnerId,
            bool captureChanged,
            ProductionFacilityCaptureResolutionFailureReason failureReason,
            IEnumerable<CapturedFacilityQueueResolution> facilities)
        {
            SectorId = RequireId(sectorId, nameof(sectorId));
            PreviousOwnerId = RequireId(previousOwnerId, nameof(previousOwnerId));
            NewOwnerId = RequireId(newOwnerId, nameof(newOwnerId));
            CaptureChanged = captureChanged;
            FailureReason = failureReason;

            if (facilities == null)
            {
                throw new ArgumentNullException(nameof(facilities));
            }

            var copy = new List<CapturedFacilityQueueResolution>(facilities);
            Facilities = copy.AsReadOnly();
        }

        public bool Success => FailureReason == ProductionFacilityCaptureResolutionFailureReason.None;

        public string SectorId { get; }

        public string PreviousOwnerId { get; }

        public string NewOwnerId { get; }

        public bool CaptureChanged { get; }

        public ProductionFacilityCaptureResolutionFailureReason FailureReason { get; }

        public IReadOnlyList<CapturedFacilityQueueResolution> Facilities { get; }

        public int ProductionFacilityCount
        {
            get
            {
                var count = 0;
                foreach (var facility in Facilities)
                {
                    if (facility.WasProductionFacility)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public int CancelledOrderCount
        {
            get
            {
                var count = 0;
                foreach (var facility in Facilities)
                {
                    count += facility.CancelledOrderCount;
                }

                return count;
            }
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
