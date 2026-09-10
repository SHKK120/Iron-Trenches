using System;

namespace IronTrenches.Core
{
    public sealed class RoadPlacementRequest
    {
        public RoadPlacementRequest(
            string roadSegmentId,
            string builderFactionId,
            WorldPoint start,
            WorldPoint end,
            float width)
        {
            RoadSegmentId = RequireId(roadSegmentId, nameof(roadSegmentId));
            BuilderFactionId = RequireId(builderFactionId, nameof(builderFactionId));
            Start = start;
            End = end;
            Width = width;
        }

        public string RoadSegmentId { get; }

        public string BuilderFactionId { get; }

        public WorldPoint Start { get; }

        public WorldPoint End { get; }

        public float Width { get; }

        private static string RequireId(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("An id cannot be empty.", parameterName);
            }

            return value;
        }
    }
}
