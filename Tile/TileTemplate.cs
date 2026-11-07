using System.Diagnostics;
using DungeonGenerator.Helpers;
using DungeonGenerator.Lib;
using Spectre.Console;

namespace DungeonGenerator.Tile;

public interface ITileTemplate
{
    public abstract char Symbol { get; }
    public abstract string Name { get; }
    public abstract bool IsWalkable { get; }
    public abstract bool IsTransparent { get; }
    public abstract bool IsReplaceable { get; }
    public abstract Color Background { get; }
    public abstract Color Foreground { get; }
}

public interface IActivableTemplate : ITileTemplate
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

public interface ITorchTemplate : IActivableTemplate, IUpdateableTemplate, IActionableTemplate
{
    public abstract double Range { get; }
    public abstract bool Enabled { get; }
}

public interface IUpdateableTemplate : ITileTemplate
{
    public abstract void OnUpdate(Game game, int x, int y);
}

public interface IActionableTemplate : ITileTemplate
{
    public delegate void Action(Game game, int x, int y);
    public abstract IReadOnlyDictionary<string, Action> Actions { get; }
    public abstract string MenuTitle { get; }
    public static KeyValuePair<string, Action> Cancel { get; } = new("Cancel", delegate{});
}

public record class TileTemplate(char Symbol, string Name, bool IsWalkable, bool IsTransparent, bool IsReplaceable, Color Background, Color Foreground) : ITileTemplate
{
    // unicode symbols: https://en.wikipedia.org/wiki/Geometric%20Shapes%20(Unicode%20block)
    public static TileTemplate Wall { get; } = new('█', nameof(Wall), false, false, false, Color.Black, Color.White);
    public static TileTemplate Window { get; } = new('□', nameof(Window), false, true, false, Color.Black, Color.White);
    public static TileTemplate Ground { get; } = new('･', nameof(Ground), true, true, true, Color.Black, Color.White);
    public static TileTemplate SmallTorch(bool enabled = true) => TorchTemplate.Cache[(3, enabled)];
    public static TileTemplate Torch(bool enabled = true) => TorchTemplate.Cache[(10, enabled)];
    public static TileTemplate DownStair(bool enabled) => StairTemplate.Cache[(enabled, IStairTemplate.StairDirection.Down)];
    public static TileTemplate UpStair(bool enabled) => StairTemplate.Cache[(enabled, IStairTemplate.StairDirection.Up)];

    private record class StairTemplate(bool Enabled, IStairTemplate.StairDirection Direction)
    : TileTemplate(Direction switch
    {
        IStairTemplate.StairDirection.Up => '▲',
        IStairTemplate.StairDirection.Down => '▼',
        _ => throw new UnreachableException()
    }, "Stair", true, true, false, Color.Black, Enabled ? Color.White : Color.Gray)
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
            game.Player.Direction = Helpers.Direction.None;
            game.CurrentLevel.Update();
            return true;
        }
    }

    private record class TorchTemplate : TileTemplate, ITorchTemplate
    {
        public static DefaultableDictionary<(double, bool), TorchTemplate> Cache { get; } = new DefaultableDictionary<(double, bool), TorchTemplate>(t => new(t.Item1, t.Item2));
        public bool ActivateOnWalkOn => false;
        public string MenuTitle => "Torch";
        public IReadOnlyDictionary<string, IActionableTemplate.Action> Actions { get; }
        public double Range { get; }
        public bool Enabled { get; }

        public TorchTemplate(double range, bool enabled)
        : base('☼', "Torch", true, true, false, Color.Black, enabled ? Color.White : Color.Gray)
        {
            Range = range;
            Enabled = enabled;
            Actions = new Dictionary<string, IActionableTemplate.Action>()
            {
                ["Turn " + (enabled ? "off" : "on")] = (game, x, y) => Activate(game, x, y),
                ["Pickup"] = Pickup,
            };
        }

        private void Pickup(Game game, int x, int y)
        {
            game.CurrentLevel[x, y] = game.CurrentLevel[x, y] with { Template = Ground };
            game.Player.Items[Item.SmallTorch.Instance]++;
            game.CurrentLevel.Update();
        }

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
