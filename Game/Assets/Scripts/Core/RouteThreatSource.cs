using System;

namespace IronTrenches.Core
{
    public enum RouteThreatKind
    {
        MobileUnit,
        FortifiedPosition
    }

    public sealed class RouteThreatSource
    {
        public RouteThreatSource(
            string threatSourceId,
            string factionId,
            WorldPoint position,
            float threatRadius,
            RouteThreatKind threatKind,
            float threatWeight)
        {
            if (string.IsNullOrWhiteSpace(threatSourceId))
            {
                throw new ArgumentException(
                    "A threat source id cannot be empty.",
                    nameof(threatSourceId));
            }

            if (string.IsNullOrWhiteSpace(factionId))
            {
                throw new ArgumentException("A faction id cannot be empty.", nameof(factionId));
            }

            if (!RoadSegment.IsFinite(position))
            {
                throw new ArgumentException("A threat position must be finite.", nameof(position));
            }

            if (float.IsNaN(threatRadius)
                || float.IsInfinity(threatRadius)
                || threatRadius <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(threatRadius),
                    "A threat radius must be finite and greater than zero.");
            }

            if (!Enum.IsDefined(typeof(RouteThreatKind), threatKind))
            {
                throw new ArgumentOutOfRangeException(nameof(threatKind));
            }

            if (float.IsNaN(threatWeight)
                || float.IsInfinity(threatWeight)
                || threatWeight < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(threatWeight),
                    "A threat weight must be finite and non-negative.");
            }

            ThreatSourceId = threatSourceId;
            FactionId = factionId;
            Position = position;
            ThreatRadius = threatRadius;
            ThreatKind = threatKind;
            ThreatWeight = threatWeight;
        }

        public string ThreatSourceId { get; }

        public string FactionId { get; }

        public WorldPoint Position { get; }

        public float ThreatRadius { get; }

        public RouteThreatKind ThreatKind { get; }

        public float ThreatWeight { get; }
    }
}
