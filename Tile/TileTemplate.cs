using Spectre.Console;

namespace DungeonGenerator.Tile;

public record class TileTemplate(char Symbol, bool IsWalkable, bool IsTransparent, Color Background, Color Foreground)
{
    // unicode symbols: https://en.wikipedia.org/wiki/Geometric%20Shapes%20(Unicode%20block)
    public static TileTemplate Wall { get; } = new('█', false, false, Color.Black, Color.White);
    public static TileTemplate Window { get; } = new('□', false, true, Color.Black, Color.White);
    public static TileTemplate Ground { get; } = new('.', true, true, Color.Black, Color.White);
    public static TileTemplate Downstair { get; } = new('▼', true, true, Color.Black, Color.White);
}

public record class UpstairTemplate(bool Enabled) : TileTemplate('▲', true, true, Color.Black, Enabled ? Color.White : Color.Gray);
