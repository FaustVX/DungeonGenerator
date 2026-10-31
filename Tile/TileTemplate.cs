using System.Diagnostics;
using Spectre.Console;

namespace DungeonGenerator.Tile;

public interface IActivableTemplate
{
    public abstract bool ActivateOnWalkOn { get; }
    public abstract bool Activate(Game game);
}

public interface IStairTemplate : IActivableTemplate
{
    public abstract bool Enabled { get; }
    public abstract StairDirection Direction { get; }

    public enum StairDirection
    {
        Up,
        Down,
    }
}

public record class TileTemplate(char Symbol, bool IsWalkable, bool IsTransparent, Color Background, Color Foreground)
{
    // unicode symbols: https://en.wikipedia.org/wiki/Geometric%20Shapes%20(Unicode%20block)
    public static TileTemplate Wall { get; } = new('█', false, false, Color.Black, Color.White);
    public static TileTemplate Window { get; } = new('□', false, true, Color.Black, Color.White);
    public static TileTemplate Ground { get; } = new('･', true, true, Color.Black, Color.White);
    public static TileTemplate DownStair(bool enabled) => new StairTemplate(enabled, IStairTemplate.StairDirection.Down);
    public static TileTemplate UpStair(bool enabled) => new StairTemplate(enabled, IStairTemplate.StairDirection.Up);

    private record class StairTemplate(bool Enabled, IStairTemplate.StairDirection Direction)
    : TileTemplate(Direction switch
    {
        IStairTemplate.StairDirection.Up => '▲',
        IStairTemplate.StairDirection.Down => '▼',
        _ => throw new UnreachableException()
    }, true, true, Color.Black, Enabled ? Color.White : Color.Gray)
    , IStairTemplate
    {
        public bool ActivateOnWalkOn => true;

        public bool Activate(Game game)
        {
            if (!Enabled)
                return false;
            game.Steps++;
            game.Depth += Direction switch
            {
                IStairTemplate.StairDirection.Up => -1,
                IStairTemplate.StairDirection.Down => 1,
                _ => throw new UnreachableException(),
            };
            (game.Player.X, game.Player.Y) = game.CurrentLevel.MainTiles[Direction switch
            {
                IStairTemplate.StairDirection.Up => Level.MainTile.Exit,
                IStairTemplate.StairDirection.Down =>Level.MainTile.Entrance,
                _ => throw new UnreachableException(),
            }];
            game.CurrentLevel.Update();
            return true;
        }
    }
}
