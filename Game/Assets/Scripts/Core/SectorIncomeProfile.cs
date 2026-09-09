using System;

namespace IronTrenches.Core
{
    public sealed class SectorIncomeProfile
    {
        public SectorIncomeProfile(string sectorId, long incomeValue)
        {
            if (string.IsNullOrWhiteSpace(sectorId))
            {
                throw new ArgumentException("An id cannot be empty.", nameof(sectorId));
            }

            if (incomeValue < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(incomeValue),
                    "Sector income cannot be negative.");
            }

            SectorId = sectorId;
            IncomeValue = incomeValue;
        }

        public string SectorId { get; }

        public long IncomeValue { get; }
    }
}
