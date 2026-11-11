using DungeonGenerator.Helpers;
using DungeonGenerator.Lib;
using Spectre.Console;

namespace DungeonGenerator.Creature;

public record class Player(int X, int Y) : Creature(X, Y, "You", '@', 5, Color.Aqua)
{
    public double ViewDistance => Equipments.TryGetValue(EquipmentType.Torch, out Item.Torch? t) ? t.Range : 1;
    public Direction Direction { get; set; }
    public Dictionary<EquipmentType, Item.Item> Equipments { get; } = [];
    public override int Force => 2;

    public IDictionary<Item.Item, int> Items { get; } = new DefaultableDictionary<Item.Item, int>(capacity: 10, _ => 0);

    public override void Defend(Creature attacker, int attackPoints)
    {
        HP -= attackPoints;
    }

    public class EquipMessage(Game game, Item.Item item) : Messages.Message(game)
    {
        public override string ToString()
        => $"{_timestamp} {PlayerColor()} have equiped {ItemColor(item)}";
    }
}

public enum EquipmentType
{
    Torch,
}
