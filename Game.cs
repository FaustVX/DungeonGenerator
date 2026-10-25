using DungeonGenerator.Lib;

namespace DungeonGenerator;

public sealed class Game
{
    public int Width { get; } = 80;
    public int Height { get; } = 20;
    private readonly DefaultableList<Level> _levels;
    public int Depth { get; private set; } = 0;
    public Level CurrentLevel => _levels[Depth];
    public int Seed { get; }

    public Game(int seed)
    {
        _levels = [with(() => new(this, Depth), capacity: 8)];
        Seed = seed;
    }
}
