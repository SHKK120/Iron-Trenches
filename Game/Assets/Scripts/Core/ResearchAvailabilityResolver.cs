using System;

namespace IronTrenches.Core
{
    public enum ResearchAvailability
    {
        Locked,
        Available,
        Active,
        Completed
    }

    public static class ResearchAvailabilityResolver
    {
        public static ResearchAvailability Resolve(
            ResearchCatalog catalog,
            FactionResearchState state,
            string researchId)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var definition = catalog.GetRequired(researchId);
            if (state.IsCompleted(definition.ResearchId))
            {
                return ResearchAvailability.Completed;
            }

            if (state.ActiveResearch != null
                && string.Equals(
                    state.ActiveResearch.ResearchId,
                    definition.ResearchId,
                    StringComparison.Ordinal))
            {
                return ResearchAvailability.Active;
            }

            foreach (var prerequisiteId in definition.PrerequisiteResearchIds)
            {
                if (!state.IsCompleted(prerequisiteId))
                {
                    return ResearchAvailability.Locked;
                }
            }

            return ResearchAvailability.Available;
        }
    }
}
