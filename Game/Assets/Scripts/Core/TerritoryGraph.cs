using System;
using System.Collections.Generic;

namespace IronTrenches.Core
{
    public sealed class TerritoryGraph
    {
        private readonly Dictionary<string, SectorState> sectorById;
        private readonly Dictionary<string, SectorControlAnchor> anchorById;
        private readonly Dictionary<string, SectorControlAnchor> anchorBySectorId;

        public TerritoryGraph(
            SectorTopology topology,
            IEnumerable<SectorState> sectors,
            IEnumerable<SectorControlAnchor> anchors)
        {
            Topology = topology ?? throw new ArgumentNullException(nameof(topology));
            sectorById = IndexSectors(sectors);
            (anchorById, anchorBySectorId) = IndexAnchors(anchors);
        }

        public SectorTopology Topology { get; }

        public SectorState GetSector(string sectorId)
        {
            sectorId = RequireId(sectorId, nameof(sectorId));
            SectorState? sector;
            if (!sectorById.TryGetValue(sectorId, out sector))
            {
                throw new KeyNotFoundException($"Sector '{sectorId}' is not registered in this territory.");
            }

            return sector;
        }

        public SectorControlAnchor GetControlAnchorForSector(string sectorId)
        {
            sectorId = RequireId(sectorId, nameof(sectorId));
            SectorControlAnchor? anchor;
            if (!anchorBySectorId.TryGetValue(sectorId, out anchor))
            {
                throw new KeyNotFoundException($"Sector '{sectorId}' has no control anchor.");
            }

            return anchor;
        }

        public SectorState GetSectorForAnchor(string anchorId)
        {
            return GetSector(GetControlAnchor(anchorId).SectorId);
        }

        public IReadOnlyCollection<FrontlineEdge> GetFrontlines()
        {
            return FrontlineResolver.Resolve(Topology, sectorById.Values);
        }

        public CaptureResult CompleteAnchorCapture(string anchorId, string newOwnerId)
        {
            return SectorCapture.Complete(GetSectorForAnchor(anchorId), newOwnerId);
        }

        private SectorControlAnchor GetControlAnchor(string anchorId)
        {
            anchorId = RequireId(anchorId, nameof(anchorId));
            SectorControlAnchor? anchor;
            if (!anchorById.TryGetValue(anchorId, out anchor))
            {
                throw new KeyNotFoundException($"Control anchor '{anchorId}' is not registered.");
            }

            return anchor;
        }

        private Dictionary<string, SectorState> IndexSectors(IEnumerable<SectorState> sectors)
        {
            if (sectors == null)
            {
                throw new ArgumentNullException(nameof(sectors));
            }

            var indexed = new Dictionary<string, SectorState>(StringComparer.Ordinal);
            foreach (var sector in sectors)
            {
                if (sector == null)
                {
                    throw new ArgumentException("Sector states cannot contain null.", nameof(sectors));
                }

                if (!Topology.ContainsSector(sector.SectorId))
                {
                    throw new ArgumentException(
                        $"Sector '{sector.SectorId}' is not registered in the topology.",
                        nameof(sectors));
                }

                if (!indexed.TryAdd(sector.SectorId, sector))
                {
                    throw new ArgumentException(
                        $"Sector '{sector.SectorId}' has more than one state.",
                        nameof(sectors));
                }
            }

            foreach (var sectorId in Topology.SectorIds)
            {
                if (!indexed.ContainsKey(sectorId))
                {
                    throw new ArgumentException(
                        $"Registered sector '{sectorId}' has no state.",
                        nameof(sectors));
                }
            }

            return indexed;
        }

        private (
            Dictionary<string, SectorControlAnchor> ById,
            Dictionary<string, SectorControlAnchor> BySectorId) IndexAnchors(
                IEnumerable<SectorControlAnchor> anchors)
        {
            if (anchors == null)
            {
                throw new ArgumentNullException(nameof(anchors));
            }

            var byId = new Dictionary<string, SectorControlAnchor>(StringComparer.Ordinal);
            var bySectorId = new Dictionary<string, SectorControlAnchor>(StringComparer.Ordinal);
            foreach (var anchor in anchors)
            {
                if (anchor == null)
                {
                    throw new ArgumentException("Control anchors cannot contain null.", nameof(anchors));
                }

                if (!Topology.ContainsSector(anchor.SectorId))
                {
                    throw new ArgumentException(
                        $"Anchor '{anchor.AnchorId}' targets unregistered sector '{anchor.SectorId}'.",
                        nameof(anchors));
                }

                if (!byId.TryAdd(anchor.AnchorId, anchor))
                {
                    throw new ArgumentException(
                        $"Control anchor id '{anchor.AnchorId}' is already registered.",
                        nameof(anchors));
                }

                if (!bySectorId.TryAdd(anchor.SectorId, anchor))
                {
                    throw new ArgumentException(
                        $"Sector '{anchor.SectorId}' already has a control anchor.",
                        nameof(anchors));
                }
            }

            foreach (var sectorId in Topology.SectorIds)
            {
                if (!bySectorId.ContainsKey(sectorId))
                {
                    throw new ArgumentException(
                        $"Registered sector '{sectorId}' has no control anchor.",
                        nameof(anchors));
                }
            }

            return (byId, bySectorId);
        }

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
