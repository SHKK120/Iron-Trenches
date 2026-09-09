using System;

namespace IronTrenches.Core
{
    public sealed class EconomyState
    {
        public EconomyState(string factionId, long initialBalance)
        {
            if (string.IsNullOrWhiteSpace(factionId))
            {
                throw new ArgumentException("An id cannot be empty.", nameof(factionId));
            }

            if (initialBalance < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(initialBalance),
                    "Initial balance cannot be negative.");
            }

            FactionId = factionId;
            Balance = initialBalance;
        }

        public string FactionId { get; }

        public long Balance { get; private set; }

        internal void AddIncome(long amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Income cannot be negative.");
            }

            Balance = checked(Balance + amount);
        }
    }
}
