namespace IronTrenches.Core
{
    public enum ProductionEnqueueFailureReason
    {
        None,
        FactionMismatch,
        UnknownFacility,
        FacilityIncomplete,
        EnemyFacility,
        UnknownFacilityProfile,
        UnsupportedUnitType,
        UnknownUnitType,
        DuplicateOrderId,
        DuplicateReinforcementId,
        UnknownDestinationSector,
        InvalidDestination,
        InsufficientFunds
    }

    public sealed class ProductionEnqueueResult
    {
        private ProductionEnqueueResult(
            bool success,
            ProductionEnqueueFailureReason failureReason,
            ProductionOrder? order,
            long previousBalance,
            long newBalance)
        {
            Success = success;
            FailureReason = failureReason;
            Order = order;
            PreviousBalance = previousBalance;
            NewBalance = newBalance;
        }

        public bool Success { get; }

        public ProductionEnqueueFailureReason FailureReason { get; }

        public ProductionOrder? Order { get; }

        public long PreviousBalance { get; }

        public long NewBalance { get; }

        internal static ProductionEnqueueResult Succeeded(
            ProductionOrder order,
            long previousBalance,
            long newBalance)
        {
            return new ProductionEnqueueResult(
                true,
                ProductionEnqueueFailureReason.None,
                order,
                previousBalance,
                newBalance);
        }

        internal static ProductionEnqueueResult Failed(
            ProductionEnqueueFailureReason failureReason,
            long balance)
        {
            return new ProductionEnqueueResult(false, failureReason, null, balance, balance);
        }
    }
}
