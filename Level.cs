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
