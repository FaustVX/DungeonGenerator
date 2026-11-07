using DungeonGenerator.Helpers;
using DungeonGenerator.Lib;
using Spectre.Console;

namespace DungeonGenerator.Creature;

public record class Player(int X, int Y) : Creature(X, Y, "You", '@', 5, Color.Aqua)
{
    public double ViewDistance => Torch?.Range ?? 1;
    public Direction Direction { get; set; }
    public Item.Torch? Torch { get; set; }
    public override int Force => 2;

    public IDictionary<Item.Item, int> Items { get; } = new DefaultableDictionary<Item.Item, int>(capacity: 10, _ => 0);

    public override void Defend(Creature attacker, int attackPoints)
    {
        HP -= attackPoints;
    }
}
