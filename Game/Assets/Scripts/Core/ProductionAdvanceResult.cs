using System;
using System.Collections.Generic;

namespace IronTrenches.Core
{
    public enum ProductionAdvanceFailureReason
    {
        None,
        FacilityMismatch,
        OwnershipConflict,
        DuplicateReadyReinforcementId
    }

    public sealed class ProductionAdvanceResult
    {
        internal ProductionAdvanceResult(
            ProductionAdvanceFailureReason failureReason,
            float appliedSeconds,
            IEnumerable<ReadyReinforcement> completedReinforcements)
        {
            if (completedReinforcements == null)
            {
                throw new ArgumentNullException(nameof(completedReinforcements));
            }

            FailureReason = failureReason;
            AppliedSeconds = appliedSeconds;
            var copy = new List<ReadyReinforcement>(completedReinforcements);
            CompletedReinforcements = copy.AsReadOnly();
        }

        public bool Success => FailureReason == ProductionAdvanceFailureReason.None;

        public ProductionAdvanceFailureReason FailureReason { get; }

        public float AppliedSeconds { get; }

        public IReadOnlyCollection<ReadyReinforcement> CompletedReinforcements { get; }
    }
}
