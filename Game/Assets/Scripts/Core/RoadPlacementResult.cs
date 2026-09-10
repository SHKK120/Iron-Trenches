namespace IronTrenches.Core
{
    public enum RoadPlacementFailureReason
    {
        None,
        InvalidGeometry,
        NoTraversedSectors,
        UnknownSector,
        EnemyTerritory,
        DuplicateRoadSegmentId
    }

    public sealed class RoadPlacementResult
    {
        private RoadPlacementResult(
            bool success,
            RoadPlacementFailureReason failureReason,
            RoadSegment? roadSegment)
        {
            Success = success;
            FailureReason = failureReason;
            RoadSegment = roadSegment;
        }

        public bool Success { get; }

        public RoadPlacementFailureReason FailureReason { get; }

        public RoadSegment? RoadSegment { get; }

        internal static RoadPlacementResult Succeeded(RoadSegment roadSegment)
        {
            return new RoadPlacementResult(true, RoadPlacementFailureReason.None, roadSegment);
        }

        internal static RoadPlacementResult Failed(RoadPlacementFailureReason failureReason)
        {
            return new RoadPlacementResult(false, failureReason, null);
        }
    }
}
