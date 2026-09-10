using System;
using System.Collections.Generic;

namespace IronTrenches.Core
{
    public sealed class ReinforcementRoutePlan
    {
        public ReinforcementRoutePlan(
            WorldPoint source,
            WorldPoint destination,
            IEnumerable<ReinforcementRouteLeg> legs)
        {
            if (!RoadSegment.IsFinite(source) || !RoadSegment.IsFinite(destination))
            {
                throw new ArgumentException("Route endpoints must be finite.");
            }

            if (legs == null)
            {
                throw new ArgumentNullException(nameof(legs));
            }

            var copy = new List<ReinforcementRouteLeg>();
            var expectedStart = source;
            foreach (var leg in legs)
            {
                if (leg == null)
                {
                    throw new ArgumentException("Route legs cannot contain null.", nameof(legs));
                }

                if (!RoadSegment.PointsEqual(expectedStart, leg.Start))
                {
                    throw new ArgumentException("Route legs must be contiguous.", nameof(legs));
                }

                copy.Add(leg);
                expectedStart = leg.End;
            }

            if (copy.Count == 0)
            {
                throw new ArgumentException("A route requires at least one leg.", nameof(legs));
            }

            if (!RoadSegment.PointsEqual(expectedStart, destination))
            {
                throw new ArgumentException("The route does not reach its destination.", nameof(legs));
            }

            Source = source;
            Destination = destination;
            Legs = copy.AsReadOnly();

            var distance = 0f;
            foreach (var leg in copy)
            {
                distance += leg.Distance;
            }

            TotalDistance = distance;
        }

        public WorldPoint Source { get; }

        public WorldPoint Destination { get; }

        public IReadOnlyList<ReinforcementRouteLeg> Legs { get; }

        public float TotalDistance { get; }

        public float EstimateTravelTime(float baseMovementSpeed)
        {
            var total = 0f;
            foreach (var leg in Legs)
            {
                total += leg.EstimateTravelTime(baseMovementSpeed);
            }

            return total;
        }
    }
}
