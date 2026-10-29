using DungeonGenerator.Lib;
using DungeonGenerator.Tile;

namespace DungeonGenerator;

public sealed class Game
{
    public int Width { get; }
    public int Height { get; }
    private readonly DefaultableList<Level> _levels;
    private readonly Player player;

    public int Depth { get; private set; } = 0;
    public Level CurrentLevel => _levels[Depth];
    public int Seed { get; }
    public IPlayer Player => player;
    public Game(int width, int height, int seed)
    {
        _levels = [with(() => new(this, Depth), capacity: 8)];
        Width = width;
        Height = height;
        Seed = seed;
        var (x, y) = CurrentLevel.MainTiles[Level.MainTile.Entrance];
        player = new(x, y);
    }

    private bool Move(int x, int y)
    {
        if (!CurrentLevel[x, y].Template.IsWalkable)
            return false;
        if (CurrentLevel[x, y].Template == TileTemplate.Downstair)
        {
            Depth++;
            (x, y) = CurrentLevel.MainTiles[Level.MainTile.Entrance];
        }
        else if (CurrentLevel[x, y].Template is UpstairTemplate(Enabled: true))
        {
            Depth--;
            (x, y) = CurrentLevel.MainTiles[Level.MainTile.Exit];
        }
        (player.X, player.Y) = (x, y);
        return true;
    }

    public bool MoveUp()
    => Move(player.X, player.Y - 1);

    public bool MoveDown()
    => Move(player.X, player.Y + 1);

    public bool MoveLeft()
    => Move(player.X - 1, player.Y);

    public bool MoveRight()
    => Move(player.X + 1, player.Y);
}
