using DungeonGenerator.Helpers;

namespace DungeonGenerator.Item;

public interface IEquipItem
{
    public static abstract Creature.EquipmentType EquipmentType { get; }
    public abstract IReadOnlyDictionary<string, Item.Action> EquipedActions { get; }

    public static void Equip<TItem>(Game game, IDictionary<Item, int> items, Item item)
    where TItem : Item, IEquipItem
    {
        if (!game.Player.Equipments.ContainsKey(TItem.EquipmentType))
        {
            game.Player.Equipments[TItem.EquipmentType] = item;
            game.Messages.Add(new Creature.Player.EquipMessage<TItem>(game, (TItem)item));
            items.RemoveQuantity(item, 1);
            game.CurrentLevel.UpdateLight();
        }
    }

    public static void Unequip<TItem>(Game game, IDictionary<Item, int> _, Item item)
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
}
