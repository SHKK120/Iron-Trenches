namespace IronTrenches.Core
{
    public enum ReinforcementDispatchFailureReason
    {
        None,
        FactionMismatch,
        UnknownSourceSector,
        UnknownDestinationSector,
        SourceNotSupplied,
        DestinationNotOwned,
        DestinationCutOff,
        DuplicateReinforcementId
    }

    public sealed class ReinforcementDispatchResult
    {
        private ReinforcementDispatchResult(
            bool success,
            ReinforcementDispatchFailureReason failureReason,
            ReinforcementTransit? transit)
        {
            Success = success;
            FailureReason = failureReason;
            Transit = transit;
        }

        public bool Success { get; }

        public ReinforcementDispatchFailureReason FailureReason { get; }

        public ReinforcementTransit? Transit { get; }

        internal static ReinforcementDispatchResult Succeeded(ReinforcementTransit transit)
        {
            return new ReinforcementDispatchResult(
                true,
                ReinforcementDispatchFailureReason.None,
                transit);
        }

        internal static ReinforcementDispatchResult Failed(
            ReinforcementDispatchFailureReason failureReason)
        {
            return new ReinforcementDispatchResult(false, failureReason, null);
        }
    }
}
