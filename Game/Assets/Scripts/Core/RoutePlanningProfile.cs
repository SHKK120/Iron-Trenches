using System;

namespace IronTrenches.Core
{
    public sealed class RoutePlanningProfile
    {
        public RoutePlanningProfile(double travelTimeWeight, double threatWeight)
        {
            if (!IsFiniteNonNegative(travelTimeWeight))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(travelTimeWeight),
                    "Travel-time weight must be finite and non-negative.");
            }

            if (!IsFiniteNonNegative(threatWeight))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(threatWeight),
                    "Threat weight must be finite and non-negative.");
            }

            if (travelTimeWeight == 0d && threatWeight == 0d)
            {
                throw new ArgumentException("At least one route-planning weight must be positive.");
            }

            TravelTimeWeight = travelTimeWeight;
            ThreatWeight = threatWeight;
        }

        public double TravelTimeWeight { get; }

        public double ThreatWeight { get; }

        public double Score(double estimatedTravelTime, double totalThreat)
        {
            if (!IsFiniteNonNegative(estimatedTravelTime))
            {
                throw new ArgumentOutOfRangeException(nameof(estimatedTravelTime));
            }

            if (!IsFiniteNonNegative(totalThreat))
            {
                throw new ArgumentOutOfRangeException(nameof(totalThreat));
            }

            return (estimatedTravelTime * TravelTimeWeight)
                + (totalThreat * ThreatWeight);
        }

        private static bool IsFiniteNonNegative(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0d;
        }
    }
}
