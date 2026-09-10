using System;
using System.Collections.Generic;

namespace IronTrenches.Core
{
    public sealed class ResearchDefinition
    {
        public ResearchDefinition(
            string researchId,
            long prototypeCost,
            float researchSeconds,
            IEnumerable<string> prerequisiteResearchIds)
        {
            if (string.IsNullOrWhiteSpace(researchId))
            {
                throw new ArgumentException("A research id cannot be empty.", nameof(researchId));
            }

            if (prototypeCost < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(prototypeCost),
                    "Prototype research cost cannot be negative.");
            }

            if (float.IsNaN(researchSeconds)
                || float.IsInfinity(researchSeconds)
                || researchSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(researchSeconds),
                    "Research seconds must be finite and greater than zero.");
            }

            if (prerequisiteResearchIds == null)
            {
                throw new ArgumentNullException(nameof(prerequisiteResearchIds));
            }

            var prerequisites = new List<string>();
            var prerequisiteIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var prerequisiteId in prerequisiteResearchIds)
            {
                if (string.IsNullOrWhiteSpace(prerequisiteId))
                {
                    throw new ArgumentException(
                        "Prerequisite research ids cannot contain an empty id.",
                        nameof(prerequisiteResearchIds));
                }

                if (string.Equals(researchId, prerequisiteId, StringComparison.Ordinal))
                {
                    throw new ArgumentException(
                        "Research cannot require itself.",
                        nameof(prerequisiteResearchIds));
                }

                if (!prerequisiteIds.Add(prerequisiteId))
                {
                    throw new ArgumentException(
                        $"Prerequisite research id '{prerequisiteId}' is duplicated.",
                        nameof(prerequisiteResearchIds));
                }

                prerequisites.Add(prerequisiteId);
            }

            ResearchId = researchId;
            PrototypeCost = prototypeCost;
            ResearchSeconds = researchSeconds;
            PrerequisiteResearchIds = prerequisites.AsReadOnly();
        }

        public string ResearchId { get; }

        public long PrototypeCost { get; }

        public float ResearchSeconds { get; }

        public IReadOnlyList<string> PrerequisiteResearchIds { get; }
    }
}
