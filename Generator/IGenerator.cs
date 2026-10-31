namespace DungeonGenerator.Generator;

public interface IGenerator
{
    public delegate IGenerator Ctor(int width, int height, RngOrSeed rng, Level level);
    public static IReadOnlyList<Ctor> Generators { get; } = [
        (width, height, seed, level) => new BSP(width, height, (5, 5), (10, 10), .5, seed, level),
        // (width, height, seed, level) => new BSP(width, height, (10, 10), (15, 15), .5, seed, level),
    ];

    public Tile.TileTemplate GetTile(int x, int y);
    public int Width { get; }
    public int Height { get; }
}

public readonly union RngOrSeed(Random, int)
{
    public Random Random
    => this switch
    {
        Random rng => rng,
        int seed => new(seed),
    };
}
