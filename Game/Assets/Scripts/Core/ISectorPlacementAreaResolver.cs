namespace IronTrenches.Core
{
    public interface ISectorPlacementAreaResolver
    {
        string? ResolveSector(WorldPoint point);

        bool ContainsFootprint(string sectorId, WorldPoint center, float radius);
    }
}
