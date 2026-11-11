using Spectre.Console;

namespace DungeonGenerator.Creature;

public closed record class Mob(int X, int Y, string Name, char Symbol, int HP, Color Color) : Creature(X, Y, Name, Symbol, HP, Color)
{
    public bool HasBeenSaw { get; private set; }

    private DateTime _highlight = default;
    public (int x, int y) Pos
    {
        get;
        set => (field, _highlight) = (value, DateTime.Now.AddSeconds(3));
    }
    public bool IsHighlighted => _highlight >= DateTime.Now;

    public void Saw(Game game)
    {
        if (HasBeenSaw)
            return;
        game.Messages.Add(new SawMessage(game, this));
        HasBeenSaw = true;
    }
    public abstract void Update(Level level);

    public virtual void Death(Game game)
    {
        game.CurrentLevel.Creatures.Remove(this);
        if (game.CurrentLevel.Creatures is [])
        {
            var (x, y) = game.CurrentLevel.MainTiles[Level.MainTile.Exit];
            game.CurrentLevel[x, y] = game.CurrentLevel[x, y] with { Template = Tile.TileTemplate.DownStair(true) };
            game.Messages.Add(new ExitOpenMessage(game));
        }
    }

    protected class ExitOpenMessage(Game game) : Messages.Message(game)
    {
        public override string ToString()
        => $"{_timestamp} Exit is now open";
    }

    protected class SawMessage(Game game, Mob mob) : Messages.Message(game), Messages.IClickableMessage
    {
        private readonly (int, int) _pos = (mob.X, mob.Y);
        public void OnClick(int pos)
        => mob.Pos = _pos;

        public override string ToString()
        => $"{_timestamp} {PlayerColor()} saw {CreatureColor(mob)}";
    }
}
