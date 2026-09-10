using System;

namespace IronTrenches.Core
{
    public static class ResearchService
    {
        public static ResearchStartResult Start(
            string researchId,
            ResearchCatalog catalog,
            FactionResearchState state,
            EconomyState economy)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (economy == null)
            {
                throw new ArgumentNullException(nameof(economy));
            }

            var balance = economy.Balance;
            if (!string.Equals(state.FactionId, economy.FactionId, StringComparison.Ordinal))
            {
                return ResearchStartResult.Failed(
                    ResearchStartFailureReason.FactionMismatch,
                    balance);
            }

            if (!catalog.TryGet(researchId, out var definition))
            {
                return ResearchStartResult.Failed(
                    ResearchStartFailureReason.UnknownResearch,
                    balance);
            }

            if (state.IsCompleted(definition!.ResearchId))
            {
                return ResearchStartResult.Failed(
                    ResearchStartFailureReason.AlreadyCompleted,
                    balance);
            }

            if (state.ActiveResearch != null)
            {
                var failureReason = string.Equals(
                    state.ActiveResearch.ResearchId,
                    definition.ResearchId,
                    StringComparison.Ordinal)
                    ? ResearchStartFailureReason.AlreadyActive
                    : ResearchStartFailureReason.AnotherResearchActive;
                return ResearchStartResult.Failed(failureReason, balance);
            }

            foreach (var prerequisiteId in definition.PrerequisiteResearchIds)
            {
                if (!state.IsCompleted(prerequisiteId))
                {
                    return ResearchStartResult.Failed(
                        ResearchStartFailureReason.PrerequisiteMissing,
                        balance);
                }
            }

            if (!economy.CanAfford(definition.PrototypeCost)
                || !economy.Spend(definition.PrototypeCost))
            {
                return ResearchStartResult.Failed(
                    ResearchStartFailureReason.InsufficientFunds,
                    balance);
            }

            var activeResearch = state.Begin(definition);
            return ResearchStartResult.Succeeded(
                activeResearch,
                balance,
                economy.Balance);
        }

        public static ResearchAdvanceResult Advance(
            FactionResearchState state,
            float deltaSeconds)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (float.IsNaN(deltaSeconds)
                || float.IsInfinity(deltaSeconds)
                || deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(deltaSeconds),
                    "Delta seconds must be finite and non-negative.");
            }

            if (state.ActiveResearch == null)
            {
                return new ResearchAdvanceResult(
                    ResearchAdvanceFailureReason.NoActiveResearch,
                    0f,
                    deltaSeconds,
                    null);
            }

            var appliedSeconds = state.ActiveResearch.Advance(deltaSeconds);
            var unusedSeconds = deltaSeconds - appliedSeconds;
            string? completedResearchId = null;
            if (state.ActiveResearch.RemainingSeconds == 0f)
            {
                completedResearchId = state.CompleteActive();
            }

            return new ResearchAdvanceResult(
                ResearchAdvanceFailureReason.None,
                appliedSeconds,
                unusedSeconds,
                completedResearchId);
        }
    }
}
