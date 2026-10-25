using Spectre.Console;

namespace DungeonGenerator.Tile;

public record class TileTemplate(char Symbol, bool IsWalkable, bool IsTransparent, Color Background)
{
    public static TileTemplate Wall { get; } = new('#', false, false, Color.Black);
    public static TileTemplate Window { get; } = new('□', false, true, Color.Black);
    public static TileTemplate Ground { get; } = new('.', true, true, Color.Black);
}
