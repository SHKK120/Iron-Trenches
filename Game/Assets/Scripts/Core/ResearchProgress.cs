using System;

namespace IronTrenches.Core
{
    public sealed class ResearchProgress
    {
        internal ResearchProgress(ResearchDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            ResearchId = definition.ResearchId;
            RequiredSeconds = definition.ResearchSeconds;
        }

        public string ResearchId { get; }

        public float ProgressSeconds { get; private set; }

        public float RequiredSeconds { get; }

        public float RemainingSeconds => Math.Max(0f, RequiredSeconds - ProgressSeconds);

        internal float Advance(float seconds)
        {
            var applied = Math.Min(RemainingSeconds, seconds);
            ProgressSeconds += applied;
            return applied;
        }
    }
}
