using System.Diagnostics;
using DungeonGenerator.Creature;
using DungeonGenerator.Generator;
using DungeonGenerator.Helpers;
using DungeonGenerator.Tile;

namespace DungeonGenerator;

public sealed class Level
{
    private readonly TileTemplate[,] _tiles;
    private readonly HashSet<(int x, int y)> _visibleTiles;
    private readonly HashSet<(int x, int y)> _alreadySeenTiles;
    public Dictionary<MainTile, (int x, int y)> MainTiles { get; } = [];
    public Game Game { get; }
    public int Depth { get; }

    public Level(Game game, int depth)
    {
        Game = game;
        Depth = depth;
        var generator = Random.Shared.NextItem(IGenerator.Generators)(game.Width, game.Height, game.Seed + depth, this);
        _tiles = new TileTemplate[game.Width, game.Height];
        _visibleTiles = [];
        _alreadySeenTiles = [];
        for (int x = 0; x < game.Width; x++)
            for (int y = 0; y < game.Height; y++)
            {
                var template = generator.GetTile(x, y);
                _tiles[x, y] = template;
                if (template is IStairTemplate { Direction: IStairTemplate.StairDirection.Up })
                    if (MainTiles.ContainsKey(MainTile.Entrance))
                        throw new Exception();
                    else
                        MainTiles.Add(MainTile.Entrance, (x, y));
                else if (template is IStairTemplate { Direction: IStairTemplate.StairDirection.Down })
                    if (MainTiles.ContainsKey(MainTile.Exit))
                        throw new Exception();
                    else
                        MainTiles.Add(MainTile.Exit, (x, y));
            }
        if (!MainTiles.ContainsKey(MainTile.Entrance) || !MainTiles.ContainsKey(MainTile.Exit))
            throw new Exception();
    }

    public Tile.Tile this[int x, int y]
    {
        get => new(x, y, _tiles[x, y], _visibleTiles.Contains((x, y)), _alreadySeenTiles.Contains((x, y)));
        set
        {
            _tiles[x, y] = value.Template;
            if (value.IsVisible)
            {
                _visibleTiles.Add((x, y));
                _alreadySeenTiles.Add((x, y));
            }
        }
    }

    public void Update()
    {
        _visibleTiles.Clear();
        ShadowCaster.ComputeFieldOfViewWithShadowCasting(Game.Player.X, Game.Player.Y, Game.Player.ViewDistance,
            (x, y) => !_tiles[x, y].IsTransparent, (x, y) =>
            {
                _visibleTiles.Add((x, y));
                _alreadySeenTiles.Add((x, y));
            });
        if (Game.Player.Direction is not Direction.None)
            ShadowCaster.ComputeFieldOfViewWithShadowCasting(Game.Player.X, Game.Player.Y, Game.Width + Game.Height,
                (x, y) => !(_tiles[x, y].IsTransparent && Game.Player.Direction switch
                    {
                        Direction.Up => y <= Game.Player.Y,
                        Direction.Down => y >= Game.Player.Y,
                        Direction.Left => x <= Game.Player.X,
                        Direction.Right => x >= Game.Player.X,
                        _ => throw new UnreachableException(),
                    }),
                (x, y) =>
                    {
                        switch (Game.Player.Direction)
                        {
                            case Direction.Up:
                                if (y > Game.Player.Y)
                                    return;
                                break;
                            case Direction.Down:
                                if (y < Game.Player.Y)
                                    return;
                                break;
                            case Direction.Left:
                                if (x > Game.Player.X)
                                    return;
                                break;
                            case Direction.Right:
                                if (x < Game.Player.X)
                                    return;
                                break;
                            default:
                                throw new UnreachableException();
                        }
                        _visibleTiles.Add((x, y));
                        _alreadySeenTiles.Add((x, y));
                    });

        for (int x = 0; x < Game.Width; x++)
            for (int y = 0; y < Game.Height; y++)
                if (_tiles[x, y] is IUpdateableTemplate exec)
                    exec.OnUpdate(Game, x, y);
    }

    public enum MainTile
    {
        Entrance,
        Exit,
    }
}
