using System;
using System.Collections.Generic;

namespace IronTrenches.Core
{
    public static class ProductionAdvanceService
    {
        public static ProductionAdvanceResult Advance(
            ProductionQueue queue,
            BuildingState facility,
            float deltaSeconds,
            ICollection<ReadyReinforcement> readyReinforcements)
        {
            if (queue == null)
            {
                throw new ArgumentNullException(nameof(queue));
            }

            if (facility == null)
            {
                throw new ArgumentNullException(nameof(facility));
            }

            if (float.IsNaN(deltaSeconds)
                || float.IsInfinity(deltaSeconds)
                || deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(deltaSeconds),
                    "Delta seconds must be finite and non-negative.");
            }

            if (readyReinforcements == null)
            {
                throw new ArgumentNullException(nameof(readyReinforcements));
            }

            if (!string.Equals(
                queue.FacilityBuildingId,
                facility.BuildingId,
                StringComparison.Ordinal))
            {
                return new ProductionAdvanceResult(
                    ProductionAdvanceFailureReason.FacilityMismatch,
                    0f,
                    Array.Empty<ReadyReinforcement>());
            }

            ValidateReadyReinforcements(readyReinforcements);
            var completed = new List<ReadyReinforcement>();
            var remainingSeconds = deltaSeconds;
            var appliedSeconds = 0f;

            while (queue.CurrentOrder != null && remainingSeconds > 0f)
            {
                var current = queue.CurrentOrder;
                if (!string.Equals(
                    current.RequestedByFactionId,
                    facility.OwnerId,
                    StringComparison.Ordinal))
                {
                    return new ProductionAdvanceResult(
                        ProductionAdvanceFailureReason.OwnershipConflict,
                        appliedSeconds,
                        completed);
                }

                var secondsToApply = Math.Min(remainingSeconds, current.RemainingSeconds);
                if (secondsToApply.Equals(current.RemainingSeconds)
                    && ContainsReinforcementId(readyReinforcements, current.ReinforcementId))
                {
                    return new ProductionAdvanceResult(
                        ProductionAdvanceFailureReason.DuplicateReadyReinforcementId,
                        appliedSeconds,
                        completed);
                }

                current.AddProgress(secondsToApply);
                remainingSeconds -= secondsToApply;
                appliedSeconds += secondsToApply;
                if (current.RemainingSeconds > 0f)
                {
                    break;
                }

                var ready = new ReadyReinforcement(current, facility);
                readyReinforcements.Add(ready);
                queue.RemoveCurrent();
                completed.Add(ready);
            }

            return new ProductionAdvanceResult(
                ProductionAdvanceFailureReason.None,
                appliedSeconds,
                completed);
        }

        private static void ValidateReadyReinforcements(
            IEnumerable<ReadyReinforcement> readyReinforcements)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var reinforcement in readyReinforcements)
            {
                if (reinforcement == null)
                {
                    throw new ArgumentException(
                        "Ready reinforcements cannot contain null.",
                        nameof(readyReinforcements));
                }

                if (!ids.Add(reinforcement.ReinforcementId))
                {
                    throw new ArgumentException(
                        $"Ready reinforcement id '{reinforcement.ReinforcementId}' is duplicated.",
                        nameof(readyReinforcements));
                }
            }
        }

        private static bool ContainsReinforcementId(
            IEnumerable<ReadyReinforcement> readyReinforcements,
            string reinforcementId)
        {
            foreach (var reinforcement in readyReinforcements)
            {
                if (string.Equals(
                    reinforcement.ReinforcementId,
                    reinforcementId,
                    StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
