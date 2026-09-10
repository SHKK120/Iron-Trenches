namespace IronTrenches.Core
{
    public enum ProductionReinforcementIntegrationFailureReason
    {
        None,
        ReadyReinforcementNotFound,
        SourceBuildingMissing,
        SourceOwnershipConflict,
        SourceSectorChanged,
        SourcePositionChanged,
        DispatchRejected
    }

    public sealed class ProductionReinforcementIntegrationResult
    {
        private ProductionReinforcementIntegrationResult(
            bool success,
            ProductionReinforcementIntegrationFailureReason failureReason,
            ReinforcementDispatchFailureReason dispatchFailureReason,
            ReinforcementTransit? transit)
        {
            Success = success;
            FailureReason = failureReason;
            DispatchFailureReason = dispatchFailureReason;
            Transit = transit;
        }

        public bool Success { get; }

        public ProductionReinforcementIntegrationFailureReason FailureReason { get; }

        public ReinforcementDispatchFailureReason DispatchFailureReason { get; }

        public ReinforcementTransit? Transit { get; }

        internal static ProductionReinforcementIntegrationResult Succeeded(
            ReinforcementTransit transit)
        {
            return new ProductionReinforcementIntegrationResult(
                true,
                ProductionReinforcementIntegrationFailureReason.None,
                ReinforcementDispatchFailureReason.None,
                transit);
        }

        internal static ProductionReinforcementIntegrationResult Failed(
            ProductionReinforcementIntegrationFailureReason failureReason,
            ReinforcementDispatchFailureReason dispatchFailureReason =
                ReinforcementDispatchFailureReason.None)
        {
            return new ProductionReinforcementIntegrationResult(
                false,
                failureReason,
                dispatchFailureReason,
                null);
        }
    }
}
