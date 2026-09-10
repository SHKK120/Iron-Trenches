namespace IronTrenches.Core
{
    public interface ITerrainMovementResolver
    {
        TerrainMovementProfile Resolve(WorldPoint point);
    }
}
