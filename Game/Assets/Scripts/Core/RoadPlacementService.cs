using System;
using System.Collections.Generic;

namespace IronTrenches.Core
{
    public static class RoadPlacementService
    {
        public static RoadPlacementResult Place(
            RoadPlacementRequest request,
            TerritoryGraph territory,
            IRoadPlacementAreaResolver areaResolver,
            RoadNetwork roadNetwork)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (territory == null)
            {
                throw new ArgumentNullException(nameof(territory));
            }

            if (areaResolver == null)
            {
                throw new ArgumentNullException(nameof(areaResolver));
            }

            if (roadNetwork == null)
            {
                throw new ArgumentNullException(nameof(roadNetwork));
            }

            if (!RoadSegment.IsFinite(request.Start)
                || !RoadSegment.IsFinite(request.End)
                || RoadSegment.PointsEqual(request.Start, request.End)
                || float.IsNaN(request.Width)
                || float.IsInfinity(request.Width)
                || request.Width <= 0f)
            {
                return RoadPlacementResult.Failed(RoadPlacementFailureReason.InvalidGeometry);
            }

            if (roadNetwork.Contains(request.RoadSegmentId))
            {
                return RoadPlacementResult.Failed(
                    RoadPlacementFailureReason.DuplicateRoadSegmentId);
            }

            var traversedSectorIds = areaResolver.ResolveTraversedSectors(
                request.Start,
                request.End,
                request.Width);
            if (traversedSectorIds == null || traversedSectorIds.Count == 0)
            {
                return RoadPlacementResult.Failed(
                    RoadPlacementFailureReason.NoTraversedSectors);
            }

            var uniqueIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var sectorId in traversedSectorIds)
            {
                if (string.IsNullOrWhiteSpace(sectorId)
                    || !territory.Topology.ContainsSector(sectorId))
                {
                    return RoadPlacementResult.Failed(RoadPlacementFailureReason.UnknownSector);
                }

                if (!uniqueIds.Add(sectorId))
                {
                    return RoadPlacementResult.Failed(RoadPlacementFailureReason.InvalidGeometry);
                }

                if (!string.Equals(
                    territory.GetSector(sectorId).OwnerId,
                    request.BuilderFactionId,
                    StringComparison.Ordinal))
                {
                    return RoadPlacementResult.Failed(RoadPlacementFailureReason.EnemyTerritory);
                }
            }

            var segment = new RoadSegment(
                request.RoadSegmentId,
                request.Start,
                request.End,
                request.Width,
                traversedSectorIds);
            roadNetwork.Register(segment);
            return RoadPlacementResult.Succeeded(segment);
        }
    }
}
