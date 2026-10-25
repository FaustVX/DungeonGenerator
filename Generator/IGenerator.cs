namespace DungeonGenerator.Generator;

public interface IGenerator
{
    public Tile.TileTemplate GetTile(int x, int y);
}
