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

public closed record class Creature(int X, int Y, string Name, char Symbol, int HP, Color Color) : ICreature
{
    public int X { get; set; } = X;
    public int Y { get; set; } = Y;
    public int HP { get; set => field = Math.Clamp(value, 0, MaxHP); } = HP;
    public int Shields { get; set => field = Math.Max(value, 0); }
    public int MaxHP { get; } = HP;
    public abstract int Force { get; }

    /// <returns>returns HP lost in fight</returns>
    public virtual int Defend(Creature attacker, int attackPoints)
    {
        var hp = HP;
        HP -= attackPoints;
        return hp - HP;
    }

    public void Attack(Creature attacked, Game game)
    {
        var damage = attacked.Defend(this, Force);
        game.Messages.Add(new FightMessage(game, this, attacked, damage));
        if (this is Mob { HP: <= 0 } a)
            a.Death(game);
        if (attacked is Mob { HP: <= 0 } b)
            b.Death(game);
    }

    protected class FightMessage(Game game, Creature attacker, Creature attacked, int damage) : Messages.Message(game)
    {
        public override string ToString()
        => $"{_timestamp} {CreatureColor(attacker)} fights {CreatureColor(attacked)} for {WithColor(damage, Color.Blue)} damages";
    }
}
