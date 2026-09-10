using System;
using System.Collections.Generic;

namespace IronTrenches.Core
{
    public sealed class FactionResearchState
    {
        private readonly HashSet<string> completedResearchIds =
            new HashSet<string>(StringComparer.Ordinal);

        public FactionResearchState(string factionId)
        {
            if (string.IsNullOrWhiteSpace(factionId))
            {
                throw new ArgumentException("A faction id cannot be empty.", nameof(factionId));
            }

            FactionId = factionId;
        }

        public string FactionId { get; }

        public ResearchProgress? ActiveResearch { get; private set; }

        public IReadOnlyCollection<string> CompletedResearchIds
        {
            get
            {
                var copy = new List<string>(completedResearchIds);
                copy.Sort(StringComparer.Ordinal);
                return copy.AsReadOnly();
            }
        }

        public bool IsCompleted(string researchId)
        {
            if (string.IsNullOrWhiteSpace(researchId))
            {
                throw new ArgumentException("A research id cannot be empty.", nameof(researchId));
            }

            return completedResearchIds.Contains(researchId);
        }

        internal ResearchProgress Begin(ResearchDefinition definition)
        {
            if (ActiveResearch != null)
            {
                throw new InvalidOperationException("A faction can have only one active research in the trial structure.");
            }

            ActiveResearch = new ResearchProgress(definition);
            return ActiveResearch;
        }

        internal string CompleteActive()
        {
            if (ActiveResearch == null)
            {
                throw new InvalidOperationException("There is no active research to complete.");
            }

            var researchId = ActiveResearch.ResearchId;
            if (!completedResearchIds.Add(researchId))
            {
                throw new InvalidOperationException(
                    $"Research '{researchId}' is already completed.");
            }

            ActiveResearch = null;
            return researchId;
        }
    }
}
