using System;
using System.Collections.Generic;

namespace IronTrenches.Core
{
    public sealed class ResearchCatalog
    {
        private readonly Dictionary<string, ResearchDefinition> definitions;

        public ResearchCatalog(IEnumerable<ResearchDefinition> definitions)
        {
            if (definitions == null)
            {
                throw new ArgumentNullException(nameof(definitions));
            }

            var materialized = new Dictionary<string, ResearchDefinition>(StringComparer.Ordinal);
            foreach (var definition in definitions)
            {
                if (definition == null)
                {
                    throw new ArgumentException(
                        "Research definitions cannot contain null.",
                        nameof(definitions));
                }

                if (materialized.ContainsKey(definition.ResearchId))
                {
                    throw new ArgumentException(
                        $"Research id '{definition.ResearchId}' is duplicated.",
                        nameof(definitions));
                }

                materialized.Add(definition.ResearchId, definition);
            }

            ValidatePrerequisites(materialized, nameof(definitions));
            ValidateAcyclic(materialized, nameof(definitions));
            this.definitions = materialized;
        }

        public IReadOnlyList<ResearchDefinition> Definitions
        {
            get
            {
                var copy = new List<ResearchDefinition>(definitions.Values);
                copy.Sort((left, right) =>
                    StringComparer.Ordinal.Compare(left.ResearchId, right.ResearchId));
                return copy.AsReadOnly();
            }
        }

        public bool Contains(string researchId)
        {
            return definitions.ContainsKey(RequireId(researchId));
        }

        public bool TryGet(string researchId, out ResearchDefinition? definition)
        {
            return definitions.TryGetValue(RequireId(researchId), out definition);
        }

        public ResearchDefinition GetRequired(string researchId)
        {
            var id = RequireId(researchId);
            if (!definitions.TryGetValue(id, out var definition))
            {
                throw new KeyNotFoundException($"Research id '{id}' is not registered.");
            }

            return definition;
        }

        private static void ValidatePrerequisites(
            IReadOnlyDictionary<string, ResearchDefinition> definitions,
            string parameterName)
        {
            foreach (var definition in definitions.Values)
            {
                foreach (var prerequisiteId in definition.PrerequisiteResearchIds)
                {
                    if (!definitions.ContainsKey(prerequisiteId))
                    {
                        throw new ArgumentException(
                            $"Research '{definition.ResearchId}' requires unknown research '{prerequisiteId}'.",
                            parameterName);
                    }
                }
            }
        }

        private static void ValidateAcyclic(
            IReadOnlyDictionary<string, ResearchDefinition> definitions,
            string parameterName)
        {
            var visitStates = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var researchId in definitions.Keys)
            {
                Visit(researchId, definitions, visitStates, parameterName);
            }
        }

        private static void Visit(
            string researchId,
            IReadOnlyDictionary<string, ResearchDefinition> definitions,
            IDictionary<string, int> visitStates,
            string parameterName)
        {
            if (visitStates.TryGetValue(researchId, out var state))
            {
                if (state == 1)
                {
                    throw new ArgumentException(
                        $"Research prerequisite graph contains a cycle at '{researchId}'.",
                        parameterName);
                }

                if (state == 2)
                {
                    return;
                }
            }

            visitStates[researchId] = 1;
            foreach (var prerequisiteId in definitions[researchId].PrerequisiteResearchIds)
            {
                Visit(prerequisiteId, definitions, visitStates, parameterName);
            }

            visitStates[researchId] = 2;
        }

        private static string RequireId(string researchId)
        {
            if (string.IsNullOrWhiteSpace(researchId))
            {
                throw new ArgumentException("A research id cannot be empty.", nameof(researchId));
            }

            return researchId;
        }
    }
}
