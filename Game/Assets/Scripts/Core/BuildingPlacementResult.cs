using System;

namespace IronTrenches.Core
{
    public enum BuildingPlacementFailureReason
    {
        None,
        InvalidRequest,
        InvalidPosition,
        UnknownSector,
        EnemyTerritory,
        OutsideTargetSector,
        FootprintCrossesSectorBoundary,
        Occupied,
        InsufficientFunds
    }

    public sealed class BuildingPlacementResult
    {
        private BuildingPlacementResult(
            bool success,
            BuildingPlacementFailureReason failureReason,
            ConstructionSite? constructionSite,
            long spentAmount,
            long previousBalance,
            long newBalance)
        {
            Success = success;
            FailureReason = failureReason;
            ConstructionSite = constructionSite;
            SpentAmount = spentAmount;
            PreviousBalance = previousBalance;
            NewBalance = newBalance;
        }

        public bool Success { get; }

        public BuildingPlacementFailureReason FailureReason { get; }

        public ConstructionSite? ConstructionSite { get; }

        public long SpentAmount { get; }

        public long PreviousBalance { get; }

        public long NewBalance { get; }

        internal static BuildingPlacementResult Failed(
            BuildingPlacementFailureReason reason,
            long unchangedBalance)
        {
            if (reason == BuildingPlacementFailureReason.None)
            {
                throw new ArgumentException("A failed placement requires a failure reason.", nameof(reason));
            }

            return new BuildingPlacementResult(
                false,
                reason,
                null,
                0,
                unchangedBalance,
                unchangedBalance);
        }

        internal static BuildingPlacementResult Succeeded(
            ConstructionSite constructionSite,
            long spentAmount,
            long previousBalance,
            long newBalance)
        {
            return new BuildingPlacementResult(
                true,
                BuildingPlacementFailureReason.None,
                constructionSite,
                spentAmount,
                previousBalance,
                newBalance);
        }
    }
}
