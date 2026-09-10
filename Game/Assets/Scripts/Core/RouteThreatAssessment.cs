using System;
using System.Collections.Generic;

namespace IronTrenches.Core
{
    public sealed class RouteThreatAssessment
    {
        private static readonly RouteThreatAssessment EmptyAssessment =
            new RouteThreatAssessment(0d, Array.Empty<string>(), Array.Empty<string>());

        internal RouteThreatAssessment(
            double totalThreat,
            IEnumerable<string> threatSourceIds,
            IEnumerable<string> fortifiedThreatSourceIds)
        {
            if (double.IsNaN(totalThreat)
                || double.IsInfinity(totalThreat)
                || totalThreat < 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(totalThreat));
            }

            TotalThreat = totalThreat;
            ThreatSourceIds = CopySortedUnique(threatSourceIds, nameof(threatSourceIds));
            FortifiedThreatSourceIds = CopySortedUnique(
                fortifiedThreatSourceIds,
                nameof(fortifiedThreatSourceIds));

            foreach (var fortifiedId in FortifiedThreatSourceIds)
            {
                if (!Contains(ThreatSourceIds, fortifiedId))
                {
                    throw new ArgumentException(
                        "Every fortified threat source must also be an assessed threat source.",
                        nameof(fortifiedThreatSourceIds));
                }
            }
        }

        public double TotalThreat { get; }

        public IReadOnlyCollection<string> ThreatSourceIds { get; }

        public IReadOnlyCollection<string> FortifiedThreatSourceIds { get; }

        public int ThreatSourceCount => ThreatSourceIds.Count;

        public bool HasThreat => ThreatSourceCount > 0;

        public bool HasFortifiedThreat => FortifiedThreatSourceIds.Count > 0;

        internal static RouteThreatAssessment Empty => EmptyAssessment;

        private static IReadOnlyCollection<string> CopySortedUnique(
            IEnumerable<string> ids,
            string parameterName)
        {
            if (ids == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            var unique = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in ids)
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    throw new ArgumentException("Threat source ids cannot be empty.", parameterName);
                }

                if (!unique.Add(id))
                {
                    throw new ArgumentException(
                        $"Threat source id '{id}' is duplicated.",
                        parameterName);
                }
            }

            var copy = new List<string>(unique);
            copy.Sort(StringComparer.Ordinal);
            return copy.AsReadOnly();
        }

        private static bool Contains(IEnumerable<string> ids, string candidate)
        {
            foreach (var id in ids)
            {
                if (string.Equals(id, candidate, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
