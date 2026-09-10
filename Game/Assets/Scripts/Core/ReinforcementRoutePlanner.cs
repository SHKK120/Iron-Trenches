using System;
using System.Collections.Generic;

namespace IronTrenches.Core
{
    public sealed class ReinforcementRoutePlanner
    {
        private readonly RoadNetwork roadNetwork;
        private readonly RoadMovementProfile roadProfile;
        private readonly ITerrainMovementResolver terrainResolver;

        public ReinforcementRoutePlanner(
            RoadNetwork roadNetwork,
            RoadMovementProfile roadProfile,
            ITerrainMovementResolver terrainResolver)
        {
            this.roadNetwork = roadNetwork ?? throw new ArgumentNullException(nameof(roadNetwork));
            this.roadProfile = roadProfile ?? throw new ArgumentNullException(nameof(roadProfile));
            this.terrainResolver = terrainResolver ?? throw new ArgumentNullException(nameof(terrainResolver));
        }

        public ReinforcementRoutePlan Plan(ReinforcementDispatchRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            IReadOnlyList<RoadSegment> roadPath;
            if (roadNetwork.TryFindPath(
                request.SourcePosition,
                request.DestinationPosition,
                out roadPath))
            {
                var roadLegs = new List<ReinforcementRouteLeg>();
                var cursor = request.SourcePosition;
                foreach (var segment in roadPath)
                {
                    WorldPoint next;
                    if (RoadSegment.PointsEqual(cursor, segment.Start))
                    {
                        next = segment.End;
                    }
                    else if (RoadSegment.PointsEqual(cursor, segment.End))
                    {
                        next = segment.Start;
                    }
                    else
                    {
                        throw new InvalidOperationException("Road path is not contiguous.");
                    }

                    roadLegs.Add(new ReinforcementRouteLeg(
                        cursor,
                        next,
                        ReinforcementRouteSurface.Road,
                        roadProfile.MovementMultiplier));
                    cursor = next;
                }

                return new ReinforcementRoutePlan(
                    request.SourcePosition,
                    request.DestinationPosition,
                    roadLegs);
            }

            var terrain = terrainResolver.Resolve(request.SourcePosition)
                ?? throw new InvalidOperationException("Terrain resolver returned null.");
            return new ReinforcementRoutePlan(
                request.SourcePosition,
                request.DestinationPosition,
                new[]
                {
                    new ReinforcementRouteLeg(
                        request.SourcePosition,
                        request.DestinationPosition,
                        ReinforcementRouteSurface.Offroad,
                        terrain.MovementMultiplier)
                });
        }
    }
}
