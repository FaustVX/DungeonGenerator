using DungeonGenerator.App.Helpers;
using Spectre.Console;

namespace DungeonGenerator.App.Creature;

public closed record class Creature(int X, int Y, string Name, char Symbol, int HP, double Chance, Color Color)
{
    public int X { get; set; } = X;
    public int Y { get; set; } = Y;
    public int HP { get; set => field = Math.Clamp(value, 0, MaxHP); } = HP;
    public abstract int Shields { get; }
    public int MaxHP { get; } = HP;
    public abstract int Force { get; }
    public virtual double Chance { get; set; } = Chance;

    /// <returns>returns HP lost in fight</returns>
    public virtual int Defend(Creature attacker, int attackPoints, Game game)
    {
        var hp = HP;
        if (Shields > 0 && !Random.Shared.NextBool(Chance))
        {
            game.Messages.Add(new ShieldMissed(game, this));
            HP -= attackPoints;
        }
        else
            HP -= attackPoints - Shields;
        if (this is Player p)
            Item.IDurabilityItem.Damage(p, game, EquipmentType.Armor, attackPoints);

        return hp - HP;
    }

    public void Attack(Creature attacked, Game game)
    {
        if (!Random.Shared.NextBool(Chance))
        {
            game.Messages.Add(new AttackMissed(game, this));
            return;
        }
        var damage = attacked.Defend(this, Force, game);
        if (this is Player p)
            Item.IDurabilityItem.Damage(p, game, EquipmentType.Weapon, 1);

        game.Messages.Add(new FightMessage(game, this, attacked, damage));
        if (this is Mob { HP: <= 0 } a)
            a.Death(game);
        if (attacked is Mob { HP: <= 0 } b)
            b.Death(game);
    }

    protected class FightMessage(Game game, Creature attacker, Creature attacked, int damage)
    : Messages.Message(game, $"{CreatureColor(attacker)} fights {CreatureColor(attacked)} for {WithColor(damage, Color.Blue)} damages");

    protected class AttackMissed(Game game, Creature attacker)
    : Messages.Message(game, $"{CreatureColor(attacker)} missed attack");

    protected class ShieldMissed(Game game, Creature attacker)
    : Messages.Message(game, $"{CreatureColor(attacker)} missed shields");
}
