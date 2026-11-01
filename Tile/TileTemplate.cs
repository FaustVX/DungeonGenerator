using System.Diagnostics;
using DungeonGenerator.Helpers;
using DungeonGenerator.Lib;
using Spectre.Console;

namespace DungeonGenerator.Tile;

public interface IActivableTemplate
{
    public abstract bool ActivateOnWalkOn { get; }
    public abstract bool Activate(Game game, int x, int y);
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

public interface IUpdateableTemplate
{
    public abstract void OnUpdate(Game game, int x, int y);
}

public record class TileTemplate(char Symbol, bool IsWalkable, bool IsTransparent, Color Background, Color Foreground)
{
    // unicode symbols: https://en.wikipedia.org/wiki/Geometric%20Shapes%20(Unicode%20block)
    public static TileTemplate Wall { get; } = new('█', false, false, Color.Black, Color.White);
    public static TileTemplate Window { get; } = new('□', false, true, Color.Black, Color.White);
    public static TileTemplate Ground { get; } = new('･', true, true, Color.Black, Color.White);
    public static TileTemplate SmallTorch { get; } = TorchTemplate.Cache[(3, true)];
    public static TileTemplate Torch { get; } = TorchTemplate.Cache[(10, true)];
    public static TileTemplate DownStair(bool enabled) => StairTemplate.Cache[(enabled, IStairTemplate.StairDirection.Down)];
    public static TileTemplate UpStair(bool enabled) => StairTemplate.Cache[(enabled, IStairTemplate.StairDirection.Up)];

    private record class StairTemplate(bool Enabled, IStairTemplate.StairDirection Direction)
    : TileTemplate(Direction switch
    {
        IStairTemplate.StairDirection.Up => '▲',
        IStairTemplate.StairDirection.Down => '▼',
        _ => throw new UnreachableException()
    }, true, true, Color.Black, Enabled ? Color.White : Color.Gray)
    , IStairTemplate
    {
        public static DefaultableDictionary<(bool, IStairTemplate.StairDirection), StairTemplate> Cache { get; } = new DefaultableDictionary<(bool, IStairTemplate.StairDirection), StairTemplate>(capacity: 4, t => new(t.Item1, t.Item2));
        public bool ActivateOnWalkOn => true;

        public bool Activate(Game game, int x, int y)
        {
            if (!Enabled)
            {
                game.CurrentLevel.Update();
                return false;
            }

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
            game.Player.Direction = DungeonGenerator.Direction.None;
            game.CurrentLevel.Update();
            return true;
        }
    }

    private record class TorchTemplate(double Range, bool Enabled) : TileTemplate('i', true, true, Color.Black, Enabled ? Color.White : Color.Gray), IUpdateableTemplate, IActivableTemplate
    {
        public static DefaultableDictionary<(double, bool), TorchTemplate> Cache { get; } = new DefaultableDictionary<(double, bool), TorchTemplate>(t => new(t.Item1, t.Item2));
        public bool ActivateOnWalkOn => false;

        public void OnUpdate(Game game, int x, int y)
        {
            if (!Enabled)
                return;
            ShadowCaster.ComputeFieldOfViewWithShadowCasting(x, y, Range,
                (x, y) => !game.CurrentLevel[x, y].Template.IsTransparent,
                (x, y) => game.CurrentLevel[x, y] = game.CurrentLevel[x, y] with { IsVisible = true });
        }

        public bool Activate(Game game, int x, int y)
        {
            game.CurrentLevel[x, y] = game.CurrentLevel[x, y] with { Template = Cache[(Range, !Enabled)] };
            game.CurrentLevel.Update();
            return true;
        }
    }
}
