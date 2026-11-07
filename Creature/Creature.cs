using DungeonGenerator.Helpers;
using Spectre.Console;

namespace DungeonGenerator.Creature;

public interface ICreature
{
    public abstract int X { get; set; }
    public abstract int Y { get; set; }
    public abstract char Symbol { get; }
    public abstract Color Color { get; }
    public abstract int HP { get; set; }
    public abstract int MaxHP { get; }
    public abstract int Shields { get; set; }
}

public closed record class Creature(int X, int Y, char Symbol, int HP, Color Color) : ICreature
{
    public int X { get; set; } = X;
    public int Y { get; set; } = Y;
    public int HP { get; set => field = Math.Clamp(value, 0, MaxHP); } = HP;
    public int Shields { get; set => field = Math.Max(value, 0); }
    public int MaxHP { get; } = HP;
    public abstract int Force { get; }

    public abstract void Defend(Creature attacker, int attackPoints);
    public void Attack(Creature attacked, Game game)
    {
        game.Messages.Add(new Messages.FightMessage(this, attacked));
        attacked.Defend(this, Force);
    }
}

public closed record class Mob(int X, int Y, char Symbol, int HP, Color Color) : Creature(X, Y, Symbol, HP, Color)
{
    public abstract void Update(Level level);
}

public record class Rat(int X, int Y) : Mob(X, Y, 'r', 5, Color.RosyBrown)
{
    private readonly Random _rng = new();

    public override int Force => 1;

    public override void Defend(Creature attacker, int attackPoints)
    {
        HP -= attackPoints;
    }

    public override void Update(Level level)
    {
        var (x, y) = RandomMovement(level);
        if ((level.Game.Player.X, level.Game.Player.Y) != (x, y))
            (X, Y) = (x, y);
        else
            Attack(level.Game.Player, level.Game);
    }

    private (int x, int y) RandomMovement(Level level)
    {
        int x, y;
        do
        {
            x = _rng.NextItem([-1, 0, 1]);
            y = _rng.NextItem([-1, 0, 1]);
        } while ((x, y) is not ((0, not 0) or (not 0, 0)) || !level[X + x, Y + y].Template.IsWalkable);
        return (X + x, Y + y);
    }
}
