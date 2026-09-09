using System;

namespace IronTrenches.Core
{
    public enum SquadCommandType
    {
        Move,
        AttackMove
    }

    public readonly struct WorldPoint
    {
        public WorldPoint(float x, float z)
        {
            X = x;
            Z = z;
        }

        public float X { get; }

        public float Z { get; }
    }

    public sealed class CommandOrder
    {
        public CommandOrder(string squadId, SquadCommandType type, WorldPoint destination)
        {
            if (string.IsNullOrWhiteSpace(squadId))
            {
                throw new ArgumentException("A command requires a squad id.", nameof(squadId));
            }

            SquadId = squadId;
            Type = type;
            Destination = destination;
        }

        public string SquadId { get; }

        public SquadCommandType Type { get; }

        public WorldPoint Destination { get; }
    }
}
