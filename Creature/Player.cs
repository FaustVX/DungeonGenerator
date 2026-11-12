using DungeonGenerator.Helpers;
using DungeonGenerator.Lib;
using Spectre.Console;

namespace DungeonGenerator.Creature;

public record class Player(int X, int Y) : Creature(X, Y, "You", '@', 5, Color.Aqua)
{
    public double ViewDistance => Equipments.TryGetValue(EquipmentType.Torch, out Item.Torch t) ? t.Range : 1;
    public Direction Direction { get; set; }
    public Dictionary<EquipmentType, Item.Item> Equipments { get; } = [];
    public override int Force => 2 + (Equipments.TryGetValue(EquipmentType.Sword, out Item.IWeaponItem w) ? w.Force : 0);
    public override int Shields => Equipments.TryGetValue(EquipmentType.Armor, out Item.IArmorItem a) ? a.Protection : 0;
    public IDictionary<Item.Item, int> Items { get; } = new DefaultableDictionary<Item.Item, int>(capacity: 10, _ => 0);

    public class EquipMessage<TItem>(Game game, TItem item) : Messages.Message(game)
    where TItem: Item.Item, Item.IEquipItem
    {
        public override string ToString()
        => $"{_timestamp} {PlayerColor()} have equiped a {TItem.EquipmentType}: {ItemColor(item)}";
    }

    public class UnequipMessage<TItem>(Game game, TItem item) : Messages.Message(game)
    where TItem: Item.Item, Item.IEquipItem
    {
        public override string ToString()
        => $"{_timestamp} {PlayerColor()} have unequiped a {TItem.EquipmentType}: {ItemColor(item)}";
    }
}

public enum EquipmentType
{
    None,
    Torch,
    Sword,
    Armor,
}
