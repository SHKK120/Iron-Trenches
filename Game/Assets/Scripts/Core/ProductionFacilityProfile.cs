using System;
using System.Collections.Generic;

namespace IronTrenches.Core
{
    public sealed class ProductionFacilityProfile
    {
        private readonly HashSet<string> producibleUnitTypeIds;

        public ProductionFacilityProfile(
            string buildingTypeId,
            IEnumerable<string> producibleUnitTypeIds)
        {
            if (string.IsNullOrWhiteSpace(buildingTypeId))
            {
                throw new ArgumentException("A building type id cannot be empty.", nameof(buildingTypeId));
            }

            if (producibleUnitTypeIds == null)
            {
                throw new ArgumentNullException(nameof(producibleUnitTypeIds));
            }

            this.producibleUnitTypeIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var unitTypeId in producibleUnitTypeIds)
            {
                if (string.IsNullOrWhiteSpace(unitTypeId))
                {
                    throw new ArgumentException(
                        "Producible unit type ids cannot contain an empty id.",
                        nameof(producibleUnitTypeIds));
                }

                if (!this.producibleUnitTypeIds.Add(unitTypeId))
                {
                    throw new ArgumentException(
                        $"Producible unit type id '{unitTypeId}' is duplicated.",
                        nameof(producibleUnitTypeIds));
                }
            }

            BuildingTypeId = buildingTypeId;
        }

        public string BuildingTypeId { get; }

        public IReadOnlyCollection<string> ProducibleUnitTypeIds
        {
            get
            {
                var copy = new List<string>(producibleUnitTypeIds);
                copy.Sort(StringComparer.Ordinal);
                return copy.AsReadOnly();
            }
        }

        public bool CanProduce(string unitTypeId)
        {
            if (string.IsNullOrWhiteSpace(unitTypeId))
            {
                throw new ArgumentException("A unit type id cannot be empty.", nameof(unitTypeId));
            }

            return producibleUnitTypeIds.Contains(unitTypeId);
        }
    }
}
