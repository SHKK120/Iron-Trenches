using System;
using System.Collections.Generic;

namespace IronTrenches.Core
{
    public readonly struct IncomeCollectionResult
    {
        public IncomeCollectionResult(
            string factionId,
            long collectedAmount,
            long previousBalance,
            long newBalance)
        {
            FactionId = factionId;
            CollectedAmount = collectedAmount;
            PreviousBalance = previousBalance;
            NewBalance = newBalance;
        }

        public string FactionId { get; }

        public long CollectedAmount { get; }

        public long PreviousBalance { get; }

        public long NewBalance { get; }
    }

    public static class SectorIncomeCollector
    {
        public static IncomeCollectionResult Collect(
            TerritoryGraph territory,
            IEnumerable<SectorIncomeProfile> profiles,
            EconomyState economy)
        {
            if (economy == null)
            {
                throw new ArgumentNullException(nameof(economy));
            }

            var collectedAmount = SectorIncomeResolver.Resolve(
                territory,
                profiles,
                economy.FactionId);
            var previousBalance = economy.Balance;
            economy.AddIncome(collectedAmount);

            return new IncomeCollectionResult(
                economy.FactionId,
                collectedAmount,
                previousBalance,
                economy.Balance);
        }
    }
}
