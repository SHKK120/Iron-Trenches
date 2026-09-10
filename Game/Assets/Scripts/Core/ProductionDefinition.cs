using System;

namespace IronTrenches.Core
{
    public sealed class ProductionDefinition
    {
        public ProductionDefinition(
            string unitTypeId,
            long prototypeCost,
            float productionSeconds,
            float baseMovementSpeed)
        {
            if (string.IsNullOrWhiteSpace(unitTypeId))
            {
                throw new ArgumentException("A unit type id cannot be empty.", nameof(unitTypeId));
            }

            if (prototypeCost < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(prototypeCost),
                    "Prototype production cost cannot be negative.");
            }

            if (!IsFinitePositive(productionSeconds))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(productionSeconds),
                    "Production seconds must be finite and greater than zero.");
            }

            if (!IsFinitePositive(baseMovementSpeed))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(baseMovementSpeed),
                    "Base movement speed must be finite and greater than zero.");
            }

            UnitTypeId = unitTypeId;
            PrototypeCost = prototypeCost;
            ProductionSeconds = productionSeconds;
            BaseMovementSpeed = baseMovementSpeed;
        }

        public string UnitTypeId { get; }

        public long PrototypeCost { get; }

        public float ProductionSeconds { get; }

        public float BaseMovementSpeed { get; }

        private static bool IsFinitePositive(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) && value > 0f;
        }
    }
}
