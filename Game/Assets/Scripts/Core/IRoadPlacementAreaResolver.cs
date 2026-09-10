using System.Collections.Generic;

namespace IronTrenches.Core
{
    public interface IRoadPlacementAreaResolver
    {
        IReadOnlyCollection<string> ResolveTraversedSectors(
            WorldPoint start,
            WorldPoint end,
            float width);
    }
}
