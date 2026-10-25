namespace DungeonGenerator.Generator;

public interface IGenerator
{
    public Tile.TileTemplate GetTile(int x, int y);
    public int Width { get; }
    public int Height { get; }
}
