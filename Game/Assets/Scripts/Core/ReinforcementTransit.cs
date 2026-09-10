using System;

namespace IronTrenches.Core
{
    public enum ReinforcementState
    {
        EnRoute,
        Arrived,
        DestroyedEnRoute
    }

    public sealed class ReinforcementTransit
    {
        private float progressOnCurrentLeg;

        internal ReinforcementTransit(
            ReinforcementDispatchRequest request,
            ReinforcementRoutePlan route)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            Route = route ?? throw new ArgumentNullException(nameof(route));
            ReinforcementId = request.ReinforcementId;
            FactionId = request.FactionId;
            BaseMovementSpeed = request.BaseMovementSpeed;
            State = ReinforcementState.EnRoute;
            CurrentLegIndex = 0;
            CurrentPosition = route.Source;
        }

        public string ReinforcementId { get; }

        public string FactionId { get; }

        public float BaseMovementSpeed { get; }

        public ReinforcementRoutePlan Route { get; }

        public ReinforcementState State { get; private set; }

        public int CurrentLegIndex { get; private set; }

        public float ProgressOnCurrentLeg => progressOnCurrentLeg;

        public float DistanceTravelled { get; private set; }

        public WorldPoint CurrentPosition { get; private set; }

        public void Advance(float deltaSeconds)
        {
            if (float.IsNaN(deltaSeconds)
                || float.IsInfinity(deltaSeconds)
                || deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(deltaSeconds),
                    "Delta seconds must be finite and non-negative.");
            }

            if (State != ReinforcementState.EnRoute || deltaSeconds == 0f)
            {
                return;
            }

            var remainingSeconds = deltaSeconds;
            while (remainingSeconds > 0f && State == ReinforcementState.EnRoute)
            {
                var leg = Route.Legs[CurrentLegIndex];
                var speed = BaseMovementSpeed * leg.MovementMultiplier;
                var remainingDistance = leg.Distance - progressOnCurrentLeg;
                var possibleDistance = speed * remainingSeconds;
                if (possibleDistance < remainingDistance)
                {
                    progressOnCurrentLeg += possibleDistance;
                    DistanceTravelled += possibleDistance;
                    remainingSeconds = 0f;
                    UpdateCurrentPosition(leg);
                    continue;
                }

                progressOnCurrentLeg = leg.Distance;
                DistanceTravelled += remainingDistance;
                remainingSeconds -= remainingDistance / speed;
                CurrentPosition = leg.End;
                CurrentLegIndex++;
                progressOnCurrentLeg = 0f;
                if (CurrentLegIndex >= Route.Legs.Count)
                {
                    CurrentLegIndex = Route.Legs.Count - 1;
                    State = ReinforcementState.Arrived;
                    CurrentPosition = Route.Destination;
                }
            }
        }

        public void MarkDestroyedEnRoute()
        {
            if (State != ReinforcementState.EnRoute)
            {
                throw new InvalidOperationException(
                    "Only an en-route reinforcement can be destroyed in transit.");
            }

            State = ReinforcementState.DestroyedEnRoute;
        }

        private void UpdateCurrentPosition(ReinforcementRouteLeg leg)
        {
            var ratio = progressOnCurrentLeg / leg.Distance;
            CurrentPosition = new WorldPoint(
                leg.Start.X + ((leg.End.X - leg.Start.X) * ratio),
                leg.Start.Z + ((leg.End.Z - leg.Start.Z) * ratio));
        }
    }
}
