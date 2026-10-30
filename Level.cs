using DungeonGenerator.Generator;
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
                if (template is UpstairTemplate)
                    if (MainTiles.ContainsKey(MainTile.Entrance))
                        throw new Exception();
                    else
                        MainTiles.Add(MainTile.Entrance, (x, y));
                else if (template == TileTemplate.Downstair)
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
        set => _tiles[x, y] = value.Template;
    }

    public void Update()
    {
        _visibleTiles.Clear();
        for (int x = 0; x < Game.Width; x++)
        {
            AddLineOfSight(Game.Player, x, 0, _tiles, _visibleTiles, _alreadySeenTiles);
            AddLineOfSight(Game.Player, x, Game.Height - 1, _tiles, _visibleTiles, _alreadySeenTiles);
        }
        for (int y = 0; y < Game.Height; y++)
        {
            AddLineOfSight(Game.Player, 0, y, _tiles, _visibleTiles, _alreadySeenTiles);
            AddLineOfSight(Game.Player, Game.Width - 1, y, _tiles, _visibleTiles, _alreadySeenTiles);
        }

        static void AddLineOfSight(IPlayer player, int x, int y, TileTemplate[,] tiles, HashSet<(int x, int y)> visibleTiles, HashSet<(int x, int y)> alreadySeenTiles)
        {
            foreach (var pos in Ext.Line(player.X, player.Y, x, y))
            {
                visibleTiles.Add(pos);
                alreadySeenTiles.Add(pos);
                var distSqrt = pos.DistanceSquared(player.X, player.Y);
                if (!tiles[pos.x, pos.y].IsTransparent || distSqrt > player.ViewDistance * player.ViewDistance)
                    break;
            }
        }
    }

    public enum MainTile
    {
        Entrance,
        Exit,
    }
}
