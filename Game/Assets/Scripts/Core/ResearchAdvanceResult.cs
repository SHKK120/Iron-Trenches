namespace IronTrenches.Core
{
    public enum ResearchAdvanceFailureReason
    {
        None,
        NoActiveResearch
    }

    public sealed class ResearchAdvanceResult
    {
        internal ResearchAdvanceResult(
            ResearchAdvanceFailureReason failureReason,
            float appliedSeconds,
            float unusedSeconds,
            string? completedResearchId)
        {
            FailureReason = failureReason;
            AppliedSeconds = appliedSeconds;
            UnusedSeconds = unusedSeconds;
            CompletedResearchId = completedResearchId;
        }

        public bool Success => FailureReason == ResearchAdvanceFailureReason.None;

        public ResearchAdvanceFailureReason FailureReason { get; }

        public float AppliedSeconds { get; }

        public float UnusedSeconds { get; }

        public string? CompletedResearchId { get; }

        public bool Completed => CompletedResearchId != null;
    }
}
