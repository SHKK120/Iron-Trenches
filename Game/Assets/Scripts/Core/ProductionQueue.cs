using System;
using System.Collections.Generic;

namespace IronTrenches.Core
{
    public sealed class ProductionQueue
    {
        private readonly List<ProductionOrder> orders = new List<ProductionOrder>();
        private readonly HashSet<string> knownOrderIds = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> knownReinforcementIds = new HashSet<string>(StringComparer.Ordinal);

        public ProductionQueue(string facilityBuildingId)
        {
            if (string.IsNullOrWhiteSpace(facilityBuildingId))
            {
                throw new ArgumentException(
                    "A facility building id cannot be empty.",
                    nameof(facilityBuildingId));
            }

            FacilityBuildingId = facilityBuildingId;
        }

        public string FacilityBuildingId { get; }

        public IReadOnlyList<ProductionOrder> Orders => orders.AsReadOnly();

        public ProductionOrder? CurrentOrder => orders.Count == 0 ? null : orders[0];

        internal void Enqueue(ProductionOrder order)
        {
            if (order == null)
            {
                throw new ArgumentNullException(nameof(order));
            }

            if (!string.Equals(
                order.FacilityBuildingId,
                FacilityBuildingId,
                StringComparison.Ordinal))
            {
                throw new ArgumentException("Order targets another facility.", nameof(order));
            }

            if (!knownOrderIds.Add(order.OrderId))
            {
                throw new ArgumentException(
                    $"Production order id '{order.OrderId}' is already known.",
                    nameof(order));
            }

            if (!knownReinforcementIds.Add(order.ReinforcementId))
            {
                knownOrderIds.Remove(order.OrderId);
                throw new ArgumentException(
                    $"Reinforcement id '{order.ReinforcementId}' is already known.",
                    nameof(order));
            }

            orders.Add(order);
        }

        internal bool Remove(ProductionOrder order)
        {
            if (!orders.Remove(order))
            {
                return false;
            }

            knownOrderIds.Remove(order.OrderId);
            knownReinforcementIds.Remove(order.ReinforcementId);
            return true;
        }

        internal bool ContainsOrderId(string orderId) => knownOrderIds.Contains(orderId);

        internal bool ContainsReinforcementId(string reinforcementId) =>
            knownReinforcementIds.Contains(reinforcementId);

        internal ProductionOrder RemoveCurrent()
        {
            if (orders.Count == 0)
            {
                throw new InvalidOperationException("The production queue is empty.");
            }

            var current = orders[0];
            orders.RemoveAt(0);
            return current;
        }

        internal IReadOnlyList<ProductionOrder> CancelAll()
        {
            var cancelled = new List<ProductionOrder>(orders);
            orders.Clear();
            return cancelled.AsReadOnly();
        }
    }
}
