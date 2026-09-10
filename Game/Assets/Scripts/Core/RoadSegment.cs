using System;
using System.Collections.Generic;

namespace IronTrenches.Core
{
    public sealed class RoadSegment
    {
        public RoadSegment(
            string roadSegmentId,
            WorldPoint start,
            WorldPoint end,
            float width,
            IEnumerable<string> traversedSectorIds)
        {
            if (string.IsNullOrWhiteSpace(roadSegmentId))
            {
                throw new ArgumentException("A road segment id cannot be empty.", nameof(roadSegmentId));
            }

            if (!IsFinite(start) || !IsFinite(end))
            {
                throw new ArgumentException("Road endpoints must be finite.");
            }

            if (PointsEqual(start, end))
            {
                throw new ArgumentException("Road endpoints must be different.", nameof(end));
            }

            if (float.IsNaN(width) || float.IsInfinity(width) || width <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(width),
                    "Road width must be finite and greater than zero.");
            }

            if (traversedSectorIds == null)
            {
                throw new ArgumentNullException(nameof(traversedSectorIds));
            }

            var uniqueIds = new HashSet<string>(StringComparer.Ordinal);
            var sectorIds = new List<string>();
            foreach (var sectorId in traversedSectorIds)
            {
                if (string.IsNullOrWhiteSpace(sectorId))
                {
                    throw new ArgumentException(
                        "Traversed sector ids cannot contain an empty id.",
                        nameof(traversedSectorIds));
                }

                if (!uniqueIds.Add(sectorId))
                {
                    throw new ArgumentException(
                        $"Traversed sector id '{sectorId}' is duplicated.",
                        nameof(traversedSectorIds));
                }

                sectorIds.Add(sectorId);
            }

            if (sectorIds.Count == 0)
            {
                throw new ArgumentException(
                    "A road must traverse at least one sector.",
                    nameof(traversedSectorIds));
            }

            RoadSegmentId = roadSegmentId;
            Start = start;
            End = end;
            Width = width;
            TraversedSectorIds = sectorIds.AsReadOnly();
        }

        public string RoadSegmentId { get; }

        public WorldPoint Start { get; }

        public WorldPoint End { get; }

        public float Width { get; }

        public IReadOnlyCollection<string> TraversedSectorIds { get; }

        public float Length
        {
            get
            {
                var deltaX = (double)End.X - Start.X;
                var deltaZ = (double)End.Z - Start.Z;
                return (float)Math.Sqrt((deltaX * deltaX) + (deltaZ * deltaZ));
            }
        }

        internal static bool IsFinite(WorldPoint point)
        {
            return !float.IsNaN(point.X)
                && !float.IsInfinity(point.X)
                && !float.IsNaN(point.Z)
                && !float.IsInfinity(point.Z);
        }

        internal static bool PointsEqual(WorldPoint first, WorldPoint second)
        {
            return first.X.Equals(second.X) && first.Z.Equals(second.Z);
        }
    }
}
