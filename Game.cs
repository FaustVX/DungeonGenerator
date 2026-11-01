using DungeonGenerator.Lib;
using DungeonGenerator.Tile;

namespace DungeonGenerator;

public sealed class Game
{
    public int Width { get; }
    public int Height { get; }
    private readonly DefaultableList<Level> _levels;
    public long Steps { get; set; }
    public int Depth { get; set; } = 0;
    public Level CurrentLevel => _levels[Depth];
    public int Seed { get; }
    public Player Player { get; }
    public Game(int width, int height, int seed)
    {
        _levels = [with(() => new(this, Depth), capacity: 8)];
        Width = width;
        Height = height;
        Seed = seed;
        var (x, y) = CurrentLevel.MainTiles[Level.MainTile.Entrance];
        Player = new(x, y);
        CurrentLevel.Update();
    }

    private bool Move(int x, int y)
    {
        if (!CurrentLevel[x, y].Template.IsWalkable)
            return false;
        Steps++;
        (Player.X, Player.Y) = (x, y);
        if (CurrentLevel[x, y].Template is IActivableTemplate { ActivateOnWalkOn: true } tile)
            return tile.Activate(this, x, y);
        CurrentLevel.Update();
        return true;
    }

    public bool MoveUp()
    => Move(Player.X, Player.Y - 1);

    public bool MoveDown()
    => Move(Player.X, Player.Y + 1);

    public bool MoveLeft()
    => Move(Player.X - 1, Player.Y);

    public bool MoveRight()
    => Move(Player.X + 1, Player.Y);

    public bool Activate()
    => CurrentLevel[Player.X, Player.Y].Template is IActivableTemplate tile && tile.Activate(this, Player.X, Player.Y);
}
