namespace DungeonGenerator.Generator;

public interface IGenerator
{
    public delegate IGenerator Ctor(int width, int height, RngOrSeed rng);
    public static IReadOnlyList<Ctor> Generators { get; } = [
        (width, height, seed) => new BSP(width, height, (3, 3), (5, 5), .5, seed),
        (width, height, seed) => new BSP(width, height, (5, 5), (15, 15), .5, seed),
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
