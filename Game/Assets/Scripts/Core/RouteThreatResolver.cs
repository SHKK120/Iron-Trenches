using System;
using System.Collections.Generic;

namespace IronTrenches.Core
{
    public static class RouteThreatResolver
    {
        public static RouteThreatAssessment Resolve(
            string factionId,
            ReinforcementRoutePlan route,
            IEnumerable<RouteThreatSource> threatSources)
        {
            if (string.IsNullOrWhiteSpace(factionId))
            {
                throw new ArgumentException("A faction id cannot be empty.", nameof(factionId));
            }

            if (route == null)
            {
                throw new ArgumentNullException(nameof(route));
            }

            if (threatSources == null)
            {
                throw new ArgumentNullException(nameof(threatSources));
            }

            var knownIds = new HashSet<string>(StringComparer.Ordinal);
            var affectingIds = new List<string>();
            var fortifiedIds = new List<string>();
            var totalThreat = 0d;
            foreach (var source in threatSources)
            {
                if (source == null)
                {
                    throw new ArgumentException(
                        "Threat sources cannot contain null.",
                        nameof(threatSources));
                }

                if (!knownIds.Add(source.ThreatSourceId))
                {
                    throw new ArgumentException(
                        $"Threat source id '{source.ThreatSourceId}' is duplicated.",
                        nameof(threatSources));
                }

                if (string.Equals(source.FactionId, factionId, StringComparison.Ordinal)
                    || !AffectsRoute(source, route))
                {
                    continue;
                }

                affectingIds.Add(source.ThreatSourceId);
                totalThreat += source.ThreatWeight;
                if (source.ThreatKind == RouteThreatKind.FortifiedPosition)
                {
                    fortifiedIds.Add(source.ThreatSourceId);
                }
            }

            return new RouteThreatAssessment(totalThreat, affectingIds, fortifiedIds);
        }

        private static bool AffectsRoute(
            RouteThreatSource source,
            ReinforcementRoutePlan route)
        {
            foreach (var leg in route.Legs)
            {
                if (DistanceToSegment(source.Position, leg.Start, leg.End)
                    <= source.ThreatRadius)
                {
                    return true;
                }
            }

            return false;
        }

        private static double DistanceToSegment(
            WorldPoint point,
            WorldPoint start,
            WorldPoint end)
        {
            var deltaX = (double)end.X - start.X;
            var deltaZ = (double)end.Z - start.Z;
            var lengthSquared = (deltaX * deltaX) + (deltaZ * deltaZ);
            var projection = ((((double)point.X - start.X) * deltaX)
                + (((double)point.Z - start.Z) * deltaZ)) / lengthSquared;
            projection = Math.Max(0d, Math.Min(1d, projection));
            var nearestX = start.X + (projection * deltaX);
            var nearestZ = start.Z + (projection * deltaZ);
            var distanceX = point.X - nearestX;
            var distanceZ = point.Z - nearestZ;
            return Math.Sqrt((distanceX * distanceX) + (distanceZ * distanceZ));
        }
    }
}
