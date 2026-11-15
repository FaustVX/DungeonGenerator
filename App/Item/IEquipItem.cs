using DungeonGenerator.App.Helpers;

namespace DungeonGenerator.App.Item;

[InitItem]
public interface IEquipItem
{
    public static abstract Creature.EquipmentType EquipmentType { get; }
    protected static string GetInfo<T>()
    where T : IEquipItem
    => $"Equipable in {T.EquipmentType} slot";

    protected static void Equip<TItem>(Game game, IDictionary<Item, int> items, Item item)
    where TItem : Item, IEquipItem
    {
        if (!game.Player.Equipments.ContainsKey(TItem.EquipmentType))
        {
            // TASK(20261115-130532-linked-equip-item-from-inventory): Equipping items from inventories are still linked
            game.Player.Equipments[TItem.EquipmentType] = item with {};
            game.Messages.Add(new Creature.Player.EquipMessage<TItem>(game, (TItem)item));
            items.RemoveQuantity(item, 1);
            game.CurrentLevel.UpdateLight();
        }
    }

    protected static void Unequip<TItem>(Game game, IDictionary<Item, int> _, Item item)
    where TItem : Item, IEquipItem
    {
        if (game.Player.Equipments.ContainsKey(TItem.EquipmentType))
        {
            game.Player.Equipments.Remove(TItem.EquipmentType);
            game.Messages.Add(new Creature.Player.UnequipMessage<TItem>(game, (TItem)item));
            game.Player.Items[item]++;
            game.CurrentLevel.UpdateLight();
        }
    }

    protected static void Init<T>()
    where T : Item, IEquipItem
    {
        Item._actionsCache[(typeof(T), ItemLocation.Ground)].Add("equip", Equip<T>);
        Item._actionsCache[(typeof(T), ItemLocation.Inventory)].Add("equip", Equip<T>);
        Item._actionsCache[(typeof(T), ItemLocation.Equipment)].Add("unequip", Unequip<T>);
    }
}
