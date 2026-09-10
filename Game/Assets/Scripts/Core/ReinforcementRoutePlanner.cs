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

            return BuildCandidatePlans(request)[0];
        }

        public ReinforcementRoutePlan Plan(
            ReinforcementDispatchRequest request,
            IEnumerable<RouteThreatSource> threatSources,
            RoutePlanningProfile planningProfile,
            out RouteThreatAssessment threatAssessment)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (threatSources == null)
            {
                throw new ArgumentNullException(nameof(threatSources));
            }

            if (planningProfile == null)
            {
                throw new ArgumentNullException(nameof(planningProfile));
            }

            var sources = new List<RouteThreatSource>(threatSources);
            var candidates = BuildCandidatePlans(request);
            var selected = candidates[0];
            var selectedAssessment = RouteThreatResolver.Resolve(
                request.FactionId,
                selected,
                sources);
            var selectedTravelTime = selected.EstimateTravelTime(request.BaseMovementSpeed);
            var selectedScore = planningProfile.Score(
                selectedTravelTime,
                selectedAssessment.TotalThreat);

            for (var index = 1; index < candidates.Count; index++)
            {
                var candidate = candidates[index];
                var candidateAssessment = RouteThreatResolver.Resolve(
                    request.FactionId,
                    candidate,
                    sources);
                var candidateTravelTime = candidate.EstimateTravelTime(request.BaseMovementSpeed);
                var candidateScore = planningProfile.Score(
                    candidateTravelTime,
                    candidateAssessment.TotalThreat);
                if (candidateScore < selectedScore
                    || (candidateScore.Equals(selectedScore)
                        && candidateTravelTime < selectedTravelTime))
                {
                    selected = candidate;
                    selectedAssessment = candidateAssessment;
                    selectedTravelTime = candidateTravelTime;
                    selectedScore = candidateScore;
                }
            }

            threatAssessment = selectedAssessment;
            return selected;
        }

        private IReadOnlyList<ReinforcementRoutePlan> BuildCandidatePlans(
            ReinforcementDispatchRequest request)
        {
            var roadPaths = roadNetwork.FindPaths(
                request.SourcePosition,
                request.DestinationPosition);
            if (roadPaths.Count > 0)
            {
                var roadPlans = new List<ReinforcementRoutePlan>();
                foreach (var roadPath in roadPaths)
                {
                    roadPlans.Add(BuildRoadPlan(request, roadPath));
                }

                return roadPlans.AsReadOnly();
            }

            var terrain = terrainResolver.Resolve(request.SourcePosition)
                ?? throw new InvalidOperationException("Terrain resolver returned null.");
            return new[]
            {
                new ReinforcementRoutePlan(
                    request.SourcePosition,
                    request.DestinationPosition,
                    new[]
                    {
                        new ReinforcementRouteLeg(
                            request.SourcePosition,
                            request.DestinationPosition,
                            ReinforcementRouteSurface.Offroad,
                            terrain.MovementMultiplier)
                    })
            };
        }

        private ReinforcementRoutePlan BuildRoadPlan(
            ReinforcementDispatchRequest request,
            IEnumerable<RoadSegment> roadPath)
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
    }
}
