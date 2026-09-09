using System;
using System.Collections.Generic;

namespace IronTrenches.Core
{
    public sealed class SectorBuildingTransferResult
    {
        public SectorBuildingTransferResult(
            string sectorId,
            string previousOwnerId,
            string newOwnerId,
            bool captureChanged,
            IEnumerable<string> transferredBuildingIds)
        {
            SectorId = RequireId(sectorId, nameof(sectorId));
            PreviousOwnerId = RequireId(previousOwnerId, nameof(previousOwnerId));
            NewOwnerId = RequireId(newOwnerId, nameof(newOwnerId));
            CaptureChanged = captureChanged;

            if (transferredBuildingIds == null)
            {
                throw new ArgumentNullException(nameof(transferredBuildingIds));
            }

            var ids = new List<string>();
            foreach (var buildingId in transferredBuildingIds)
            {
                ids.Add(RequireId(buildingId, nameof(transferredBuildingIds)));
            }

            ids.Sort(StringComparer.Ordinal);
            TransferredBuildingIds = ids.AsReadOnly();
        }

        public string SectorId { get; }

        public string PreviousOwnerId { get; }

        public string NewOwnerId { get; }

        public bool CaptureChanged { get; }

        public IReadOnlyCollection<string> TransferredBuildingIds { get; }

        public int TransferredBuildingCount => TransferredBuildingIds.Count;

        private static string RequireId(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("An id cannot be empty.", parameterName);
            }

            return value;
        }
    }

    public static class TerritoryBuildingOwnershipService
    {
        public static SectorBuildingTransferResult CaptureAndTransfer(
            TerritoryGraph territory,
            string anchorId,
            string newOwnerId,
            IEnumerable<BuildingState> completedBuildings)
        {
            if (territory == null)
            {
                throw new ArgumentNullException(nameof(territory));
            }

            if (string.IsNullOrWhiteSpace(newOwnerId))
            {
                throw new ArgumentException("An owner id cannot be empty.", nameof(newOwnerId));
            }

            var buildings = ValidateAndMaterialize(completedBuildings);
            var sector = territory.GetSectorForAnchor(anchorId);
            var capture = territory.CompleteAnchorCapture(anchorId, newOwnerId);
            var transferredIds = new List<string>();

            if (capture.Changed)
            {
                foreach (var building in buildings)
                {
                    if (string.Equals(building.SectorId, sector.SectorId, StringComparison.Ordinal)
                        && !string.Equals(building.OwnerId, newOwnerId, StringComparison.Ordinal))
                    {
                        building.TransferOwnershipTo(newOwnerId);
                        transferredIds.Add(building.BuildingId);
                    }
                }
            }

            return new SectorBuildingTransferResult(
                sector.SectorId,
                capture.PreviousOwnerId,
                capture.NewOwnerId,
                capture.Changed,
                transferredIds);
        }

        private static List<BuildingState> ValidateAndMaterialize(
            IEnumerable<BuildingState> completedBuildings)
        {
            if (completedBuildings == null)
            {
                throw new ArgumentNullException(nameof(completedBuildings));
            }

            var buildings = new List<BuildingState>();
            var buildingIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var building in completedBuildings)
            {
                if (building == null)
                {
                    throw new ArgumentException(
                        "Completed buildings cannot contain null.",
                        nameof(completedBuildings));
                }

                if (!buildingIds.Add(building.BuildingId))
                {
                    throw new ArgumentException(
                        $"Completed building id '{building.BuildingId}' is duplicated.",
                        nameof(completedBuildings));
                }

                buildings.Add(building);
            }

            return buildings;
        }
    }
}
