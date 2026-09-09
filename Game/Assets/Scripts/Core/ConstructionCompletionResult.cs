using System;

namespace IronTrenches.Core
{
    public enum ConstructionCompletionFailureReason
    {
        None,
        InvalidSiteId,
        InvalidBuildingId,
        UnknownSite,
        DuplicateBuildingId
    }

    public sealed class ConstructionCompletionResult
    {
        private ConstructionCompletionResult(
            bool success,
            ConstructionCompletionFailureReason failureReason,
            string? completedSiteId,
            BuildingState? building)
        {
            Success = success;
            FailureReason = failureReason;
            CompletedSiteId = completedSiteId;
            Building = building;
        }

        public bool Success { get; }

        public ConstructionCompletionFailureReason FailureReason { get; }

        public string? CompletedSiteId { get; }

        public BuildingState? Building { get; }

        internal static ConstructionCompletionResult Failed(
            ConstructionCompletionFailureReason reason)
        {
            if (reason == ConstructionCompletionFailureReason.None)
            {
                throw new ArgumentException("A failed completion requires a failure reason.", nameof(reason));
            }

            return new ConstructionCompletionResult(false, reason, null, null);
        }

        internal static ConstructionCompletionResult Succeeded(
            string completedSiteId,
            BuildingState building)
        {
            return new ConstructionCompletionResult(
                true,
                ConstructionCompletionFailureReason.None,
                completedSiteId,
                building);
        }
    }
}
