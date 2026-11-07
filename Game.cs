using DungeonGenerator.Creature;
using DungeonGenerator.Helpers;
using DungeonGenerator.Lib;
using DungeonGenerator.Messages;
using DungeonGenerator.Tile;

namespace DungeonGenerator;

public sealed class Game
{
    public int Width { get; }
    public int Height { get; }
    private readonly DefaultableList<Level> _levels;
    public long Steps { get; set; }
    public int Depth
    {
        get;
        set
        {
            if (value > _levels.Count || value < 0)
                return;
            field = value;
            if (Depth == _levels.Count)
                Messages.Add(new NewFloorMessage(CurrentLevel));
        }
    } = 0;
    public Level CurrentLevel => _levels[Depth];
    public int Seed { get; }
    public Player Player { get; }
    public Tile.Tile CurrentTile
    {
        get => CurrentLevel[Player.X, Player.Y];
        set => CurrentLevel[Player.X, Player.Y] = value;
    }
    public RollingList<Message> Messages { get; }

    public Game(int width, int height, int seed)
    {
        Messages = [with(20), "Welcome to the dungeon, have fun", "[blue]<space>[/] to activate main action", "[blue]<enter>[/] to see actions", "[blue]<e>[/] for inventory"];
        Width = width;
        Height = height;
        Seed = seed;
        _levels = [with(() => new(this, Depth), capacity: 8)];
        var (x, y) = CurrentLevel.MainTiles[Level.MainTile.Entrance];
        Player = new(x, y);
        CurrentLevel.Update();
    }

    private bool Move(int x, int y)
    {
        if (!CurrentLevel[x, y].Template.IsWalkable)
            return false;
        Steps++;
        if (y < Player.Y)
            Player.Direction = Direction.Up;
        else if (y > Player.Y)
            Player.Direction = Direction.Down;
        else if (x < Player.X)
            Player.Direction = Direction.Left;
        else if (x > Player.X)
            Player.Direction = Direction.Right;
        else
            Player.Direction = Direction.None;
        if (CurrentLevel.Creatures.FirstOrDefault(c => (c.X, c.Y) == (x, y)) is {} c)
        {
            Player.Attack(c, this);
            if (c.HP <= 0)
                CurrentLevel.Creatures.Remove(c);
        }
        else
        {
            (Player.X, Player.Y) = (x, y);
            if (CurrentLevel[x, y] is { Items.Count: 0, Template: IActivableTemplate { ActivateOnWalkOn: true } tile })
                return tile.Activate(this, x, y);
        }
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
    => CurrentTile.Template is IActivableTemplate tile && tile.Activate(this, Player.X, Player.Y);

    private class NewFloorMessage(Level level) : Message(level.Game)
    {
        public override string ToString()
        => $"{_timestamp} {PlayerColor()} went down to level [blue]{level.Depth}[/]";
    }
}
