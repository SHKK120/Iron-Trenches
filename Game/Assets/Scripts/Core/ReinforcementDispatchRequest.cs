using System;

namespace IronTrenches.Core
{
    public sealed class ReinforcementDispatchRequest
    {
        public ReinforcementDispatchRequest(
            string reinforcementId,
            string factionId,
            string sourceSectorId,
            WorldPoint sourcePosition,
            string destinationSectorId,
            WorldPoint destinationPosition,
            float baseMovementSpeed)
        {
            ReinforcementId = RequireId(reinforcementId, nameof(reinforcementId));
            FactionId = RequireId(factionId, nameof(factionId));
            SourceSectorId = RequireId(sourceSectorId, nameof(sourceSectorId));
            DestinationSectorId = RequireId(destinationSectorId, nameof(destinationSectorId));

            if (!RoadSegment.IsFinite(sourcePosition)
                || !RoadSegment.IsFinite(destinationPosition))
            {
                throw new ArgumentException("Reinforcement positions must be finite.");
            }

            if (RoadSegment.PointsEqual(sourcePosition, destinationPosition))
            {
                throw new ArgumentException(
                    "Reinforcement source and destination positions must be different.",
                    nameof(destinationPosition));
            }

            if (float.IsNaN(baseMovementSpeed)
                || float.IsInfinity(baseMovementSpeed)
                || baseMovementSpeed <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(baseMovementSpeed),
                    "Base movement speed must be finite and greater than zero.");
            }

            SourcePosition = sourcePosition;
            DestinationPosition = destinationPosition;
            BaseMovementSpeed = baseMovementSpeed;
        }

        public string ReinforcementId { get; }

        public string FactionId { get; }

        public string SourceSectorId { get; }

        public WorldPoint SourcePosition { get; }

        public string DestinationSectorId { get; }

        public WorldPoint DestinationPosition { get; }

        public float BaseMovementSpeed { get; }

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
