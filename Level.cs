using DungeonGenerator.Generator;
using DungeonGenerator.Tile;

namespace DungeonGenerator;

public sealed class Level
{
    private readonly TileTemplate[,] _tiles;
    public Dictionary<MainTile, (int x, int y)> MainTiles { get; } = [];
    public int Depth { get; }

    public Level(Game game, int depth)
    {
        Depth = depth;
        var generator = Random.Shared.NextItem(IGenerator.Generators)(game.Width, game.Height, game.Seed + depth, this);
        _tiles = new TileTemplate[game.Width, game.Height];
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
        get => new(x, y, _tiles[x, y], false);
        set => _tiles[x, y] = value.Template;
    }

    public enum MainTile
    {
        Entrance,
        Exit,
    }
}
