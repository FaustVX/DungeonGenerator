using Spectre.Console;

namespace DungeonGenerator.App.Creature;

public closed record class Creature(int X, int Y, string Name, char Symbol, int HP, Color Color)
{
    public int X { get; set; } = X;
    public int Y { get; set; } = Y;
    public int HP { get; set => field = Math.Clamp(value, 0, MaxHP); } = HP;
    public abstract int Shields { get; }
    public int MaxHP { get; } = HP;
    public abstract int Force { get; }

    /// <returns>returns HP lost in fight</returns>
    public virtual int Defend(Creature attacker, int attackPoints, Game game)
    {
        var hp = HP;
        HP -= attackPoints - Shields;
        if (this is Player p)
            Item.IDurabilityItem.Damage(p, game, EquipmentType.Armor, attackPoints);

        return hp - HP;
    }

    public void Attack(Creature attacked, Game game)
    {
        var damage = attacked.Defend(this, Force, game);
        if (this is Player p)
            Item.IDurabilityItem.Damage(p, game, EquipmentType.Sword, 1);

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
