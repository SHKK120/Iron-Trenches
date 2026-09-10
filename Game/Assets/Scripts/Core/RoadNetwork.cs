using System;
using System.Collections.Generic;

namespace IronTrenches.Core
{
    public sealed class RoadNetwork
    {
        private readonly Dictionary<string, RoadSegment> segmentsById =
            new Dictionary<string, RoadSegment>(StringComparer.Ordinal);

        public IReadOnlyCollection<RoadSegment> Segments
        {
            get
            {
                var segments = new List<RoadSegment>(segmentsById.Values);
                segments.Sort((left, right) => string.CompareOrdinal(
                    left.RoadSegmentId,
                    right.RoadSegmentId));
                return segments.AsReadOnly();
            }
        }

        public void Register(RoadSegment segment)
        {
            if (segment == null)
            {
                throw new ArgumentNullException(nameof(segment));
            }

            if (segmentsById.ContainsKey(segment.RoadSegmentId))
            {
                throw new ArgumentException(
                    $"Road segment id '{segment.RoadSegmentId}' is already registered.",
                    nameof(segment));
            }

            segmentsById.Add(segment.RoadSegmentId, segment);
        }

        public bool Contains(string roadSegmentId)
        {
            return segmentsById.ContainsKey(RequireId(roadSegmentId, nameof(roadSegmentId)));
        }

        public RoadSegment Get(string roadSegmentId)
        {
            roadSegmentId = RequireId(roadSegmentId, nameof(roadSegmentId));
            if (!segmentsById.TryGetValue(roadSegmentId, out var segment))
            {
                throw new KeyNotFoundException($"Road segment '{roadSegmentId}' is not registered.");
            }

            return segment;
        }

        public bool IsPointOnRoad(WorldPoint point)
        {
            if (!RoadSegment.IsFinite(point))
            {
                throw new ArgumentException("A road query point must be finite.", nameof(point));
            }

            foreach (var segment in segmentsById.Values)
            {
                if (DistanceToSegment(point, segment.Start, segment.End) <= segment.Width * 0.5f)
                {
                    return true;
                }
            }

            return false;
        }

        public IReadOnlyCollection<RoadSegment> GetConnectedSegments(string roadSegmentId)
        {
            var source = Get(roadSegmentId);
            var connected = new List<RoadSegment>();
            foreach (var candidate in segmentsById.Values)
            {
                if (ReferenceEquals(source, candidate))
                {
                    continue;
                }

                if (SharesEndpoint(source, candidate))
                {
                    connected.Add(candidate);
                }
            }

            connected.Sort((left, right) => string.CompareOrdinal(
                left.RoadSegmentId,
                right.RoadSegmentId));
            return connected.AsReadOnly();
        }

        public bool TryFindPath(
            WorldPoint source,
            WorldPoint destination,
            out IReadOnlyList<RoadSegment> path)
        {
            var paths = FindPaths(source, destination);
            if (paths.Count == 0)
            {
                path = Array.Empty<RoadSegment>();
                return false;
            }

            path = paths[0];
            return true;
        }

        public IReadOnlyList<IReadOnlyList<RoadSegment>> FindPaths(
            WorldPoint source,
            WorldPoint destination)
        {
            if (!RoadSegment.IsFinite(source) || !RoadSegment.IsFinite(destination))
            {
                throw new ArgumentException("Road path endpoints must be finite.");
            }

            if (RoadSegment.PointsEqual(source, destination))
            {
                return Array.Empty<IReadOnlyList<RoadSegment>>();
            }

            var sourceKey = new EndpointKey(source);
            var destinationKey = new EndpointKey(destination);
            var graph = BuildEndpointGraph();
            if (!graph.ContainsKey(sourceKey) || !graph.ContainsKey(destinationKey))
            {
                return Array.Empty<IReadOnlyList<RoadSegment>>();
            }

            var paths = new List<List<RoadSegment>>();
            CollectPaths(
                graph,
                sourceKey,
                destinationKey,
                new HashSet<EndpointKey> { sourceKey },
                new List<RoadSegment>(),
                paths);
            paths.Sort(ComparePaths);

            var readOnlyPaths = new List<IReadOnlyList<RoadSegment>>();
            foreach (var candidate in paths)
            {
                readOnlyPaths.Add(candidate.AsReadOnly());
            }

            return readOnlyPaths.AsReadOnly();
        }

        private Dictionary<EndpointKey, List<RoadEdge>> BuildEndpointGraph()
        {
            var graph = new Dictionary<EndpointKey, List<RoadEdge>>();
            foreach (var segment in segmentsById.Values)
            {
                var start = new EndpointKey(segment.Start);
                var end = new EndpointKey(segment.End);
                AddEdge(graph, start, new RoadEdge(end, segment));
                AddEdge(graph, end, new RoadEdge(start, segment));
            }

            foreach (var edges in graph.Values)
            {
                edges.Sort((left, right) => string.CompareOrdinal(
                    left.Segment.RoadSegmentId,
                    right.Segment.RoadSegmentId));
            }

            return graph;
        }

        private static void CollectPaths(
            IReadOnlyDictionary<EndpointKey, List<RoadEdge>> graph,
            EndpointKey current,
            EndpointKey destination,
            ISet<EndpointKey> visited,
            IList<RoadSegment> currentPath,
            ICollection<List<RoadSegment>> paths)
        {
            if (current.Equals(destination))
            {
                paths.Add(new List<RoadSegment>(currentPath));
                return;
            }

            foreach (var edge in graph[current])
            {
                if (!visited.Add(edge.Destination))
                {
                    continue;
                }

                currentPath.Add(edge.Segment);
                CollectPaths(
                    graph,
                    edge.Destination,
                    destination,
                    visited,
                    currentPath,
                    paths);
                currentPath.RemoveAt(currentPath.Count - 1);
                visited.Remove(edge.Destination);
            }
        }

        private static int ComparePaths(
            IReadOnlyCollection<RoadSegment> left,
            IReadOnlyCollection<RoadSegment> right)
        {
            var lengthComparison = GetPathLength(left).CompareTo(GetPathLength(right));
            if (lengthComparison != 0)
            {
                return lengthComparison;
            }

            return string.CompareOrdinal(GetPathKey(left), GetPathKey(right));
        }

        private static double GetPathLength(IEnumerable<RoadSegment> path)
        {
            var total = 0d;
            foreach (var segment in path)
            {
                total += segment.Length;
            }

            return total;
        }

        private static string GetPathKey(IEnumerable<RoadSegment> path)
        {
            var ids = new List<string>();
            foreach (var segment in path)
            {
                ids.Add(segment.RoadSegmentId);
            }

            return string.Join("\u001f", ids);
        }

        private static void AddEdge(
            IDictionary<EndpointKey, List<RoadEdge>> graph,
            EndpointKey endpoint,
            RoadEdge edge)
        {
            if (!graph.TryGetValue(endpoint, out var edges))
            {
                edges = new List<RoadEdge>();
                graph.Add(endpoint, edges);
            }

            edges.Add(edge);
        }

        private static float DistanceToSegment(
            WorldPoint point,
            WorldPoint start,
            WorldPoint end)
        {
            var deltaX = (double)end.X - start.X;
            var deltaZ = (double)end.Z - start.Z;
            var lengthSquared = (deltaX * deltaX) + (deltaZ * deltaZ);
            var projection = (((double)point.X - start.X) * deltaX
                + ((double)point.Z - start.Z) * deltaZ) / lengthSquared;
            projection = Math.Max(0d, Math.Min(1d, projection));
            var nearestX = start.X + (projection * deltaX);
            var nearestZ = start.Z + (projection * deltaZ);
            var distanceX = point.X - nearestX;
            var distanceZ = point.Z - nearestZ;
            return (float)Math.Sqrt((distanceX * distanceX) + (distanceZ * distanceZ));
        }

        private static bool SharesEndpoint(RoadSegment first, RoadSegment second)
        {
            return RoadSegment.PointsEqual(first.Start, second.Start)
                || RoadSegment.PointsEqual(first.Start, second.End)
                || RoadSegment.PointsEqual(first.End, second.Start)
                || RoadSegment.PointsEqual(first.End, second.End);
        }

        private static string RequireId(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("An id cannot be empty.", parameterName);
            }

            return value;
        }

        private readonly struct EndpointKey : IEquatable<EndpointKey>
        {
            public EndpointKey(WorldPoint point)
            {
                X = point.X;
                Z = point.Z;
            }

            public float X { get; }

            public float Z { get; }

            public bool Equals(EndpointKey other)
            {
                return X.Equals(other.X) && Z.Equals(other.Z);
            }

            public override bool Equals(object? obj)
            {
                return obj is EndpointKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (X.GetHashCode() * 397) ^ Z.GetHashCode();
                }
            }
        }

        private readonly struct RoadEdge
        {
            public RoadEdge(EndpointKey destination, RoadSegment segment)
            {
                Destination = destination;
                Segment = segment;
            }

            public EndpointKey Destination { get; }

            public RoadSegment Segment { get; }
        }

    }
}
