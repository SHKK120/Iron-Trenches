namespace IronTrenches.Core
{
    public enum ResearchStartFailureReason
    {
        None,
        FactionMismatch,
        UnknownResearch,
        AlreadyCompleted,
        AlreadyActive,
        AnotherResearchActive,
        PrerequisiteMissing,
        InsufficientFunds
    }

    public sealed class ResearchStartResult
    {
        private ResearchStartResult(
            ResearchStartFailureReason failureReason,
            ResearchProgress? activeResearch,
            long previousBalance,
            long newBalance)
        {
            FailureReason = failureReason;
            ActiveResearch = activeResearch;
            PreviousBalance = previousBalance;
            NewBalance = newBalance;
        }

        public bool Success => FailureReason == ResearchStartFailureReason.None;

        public ResearchStartFailureReason FailureReason { get; }

        public ResearchProgress? ActiveResearch { get; }

        public long PreviousBalance { get; }

        public long NewBalance { get; }

        internal static ResearchStartResult Succeeded(
            ResearchProgress activeResearch,
            long previousBalance,
            long newBalance)
        {
            return new ResearchStartResult(
                ResearchStartFailureReason.None,
                activeResearch,
                previousBalance,
                newBalance);
        }

        internal static ResearchStartResult Failed(
            ResearchStartFailureReason failureReason,
            long balance)
        {
            return new ResearchStartResult(failureReason, null, balance, balance);
        }
    }
}
