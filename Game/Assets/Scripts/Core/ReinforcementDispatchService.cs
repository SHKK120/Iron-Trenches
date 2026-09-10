using System;
using System.Collections.Generic;

namespace IronTrenches.Core
{
    public static class ReinforcementDispatchService
    {
        public static ReinforcementDispatchResult Dispatch(
            ReinforcementDispatchRequest request,
            TerritoryGraph territory,
            SupplyNetworkSnapshot supplySnapshot,
            ReinforcementRoutePlanner routePlanner,
            ICollection<ReinforcementTransit> existingTransits)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (territory == null)
            {
                throw new ArgumentNullException(nameof(territory));
            }

            if (supplySnapshot == null)
            {
                throw new ArgumentNullException(nameof(supplySnapshot));
            }

            if (routePlanner == null)
            {
                throw new ArgumentNullException(nameof(routePlanner));
            }

            if (existingTransits == null)
            {
                throw new ArgumentNullException(nameof(existingTransits));
            }

            if (!string.Equals(
                request.FactionId,
                supplySnapshot.FactionId,
                StringComparison.Ordinal))
            {
                return ReinforcementDispatchResult.Failed(
                    ReinforcementDispatchFailureReason.FactionMismatch);
            }

            if (!territory.Topology.ContainsSector(request.SourceSectorId))
            {
                return ReinforcementDispatchResult.Failed(
                    ReinforcementDispatchFailureReason.UnknownSourceSector);
            }

            if (!territory.Topology.ContainsSector(request.DestinationSectorId))
            {
                return ReinforcementDispatchResult.Failed(
                    ReinforcementDispatchFailureReason.UnknownDestinationSector);
            }

            foreach (var transit in existingTransits)
            {
                if (transit == null)
                {
                    throw new ArgumentException(
                        "Existing transits cannot contain null.",
                        nameof(existingTransits));
                }

                if (string.Equals(
                    transit.ReinforcementId,
                    request.ReinforcementId,
                    StringComparison.Ordinal))
                {
                    return ReinforcementDispatchResult.Failed(
                        ReinforcementDispatchFailureReason.DuplicateReinforcementId);
                }
            }

            if (supplySnapshot.GetStatus(request.SourceSectorId) != SectorSupplyStatus.Supplied)
            {
                return ReinforcementDispatchResult.Failed(
                    ReinforcementDispatchFailureReason.SourceNotSupplied);
            }

            var destinationStatus = supplySnapshot.GetStatus(request.DestinationSectorId);
            if (destinationStatus == SectorSupplyStatus.NotOwned)
            {
                return ReinforcementDispatchResult.Failed(
                    ReinforcementDispatchFailureReason.DestinationNotOwned);
            }

            if (destinationStatus == SectorSupplyStatus.CutOff)
            {
                return ReinforcementDispatchResult.Failed(
                    ReinforcementDispatchFailureReason.DestinationCutOff);
            }

            var transitToAdd = new ReinforcementTransit(request, routePlanner.Plan(request));
            existingTransits.Add(transitToAdd);
            return ReinforcementDispatchResult.Succeeded(transitToAdd);
        }
    }
}
