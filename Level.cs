using DungeonGenerator.Generator;

namespace DungeonGenerator;

public sealed class Level
{
    private readonly Tile.Tile[,] _tiles;

    public Level(Game game, int depth)
    {
        var generator = Random.Shared.NextItem(IGenerator.Generators)(game.Width, game.Height, game.Seed + depth);
        _tiles = new Tile.Tile[game.Width, game.Height];
        for (int x = 0; x < game.Width; x++)
            for (int y = 0; y < game.Height; y++)
                _tiles[x, y] = new(x, y, generator.GetTile(x, y), true);
    }

    public Tile.Tile this[int x, int y]
    {
        get => _tiles[x, y];
        set => _tiles[x, y] = value with { X = x, Y = y };
    }
}
