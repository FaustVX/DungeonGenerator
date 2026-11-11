using DungeonGenerator.Helpers;
using DungeonGenerator.Tile;
using Spectre.Console;

namespace DungeonGenerator.Item;

public abstract record class Item(string Name, char Symbol, Color Color)
{
    public class DropMessage(Game game, Creature.Creature creature, int qty, Item item) : Messages.Message(game)
    {
        public override string ToString()
        => $"{_timestamp} {CreatureColor(creature)} dropped {qty} {ItemColor(item)} on the ground";
    }

    public class PickupMessage(Game game, Creature.Creature creature, int qty, Item item) : Messages.Message(game)
    {
        public override string ToString()
        => $"{_timestamp} {CreatureColor(creature)} picked up {qty} {ItemColor(item)} from the ground";
    }

    public delegate void Action(Game game, IDictionary<Item, int> items, Item item);
    public abstract IReadOnlyDictionary<string, Action> Actions { get; }
    public static KeyValuePair<string, Action> Cancel { get; } = new("Cancel", delegate{});
    public static KeyValuePair<string, Action> Delete { get; } = new("Delete", DeleteAction);
    public static KeyValuePair<string, Action> Drop1 { get; } = new("Drop 1", Drop1Action);
    public static KeyValuePair<string, Action> DropAll { get; } = new("Drop all", DropAllAction);
    public static KeyValuePair<string, Action> Pickup { get; } = new("Pickup", PickupAction);

    protected static void DeleteAction(Game game, IDictionary<Item, int> items, Item item)
    {
        items.RemoveQuantity(item, 1);
    }

    protected static void Drop1Action(Game game, IDictionary<Item, int> items, Item item)
    {
        items.RemoveQuantity(item, 1);
        game.CurrentLevel.DropItem(game.Player.X, game.Player.Y, item, 1, game.Player);
    }

    protected static void DropAllAction(Game game, IDictionary<Item, int> items, Item item)
    {
        var qty = items[item];
        items.Remove(item);
        game.CurrentLevel.DropItem(game.Player.X, game.Player.Y, item, qty, game.Player);
    }

    protected static void PickupAction(Game game, IDictionary<Item, int> items, Item item)
    {
        var qty = game.CurrentLevel.PickupItem(game.Player.X, game.Player.Y, item);
        game.Player.Items[item] += qty;
        game.Messages.Add(new PickupMessage(game, game.Player, qty, item));
    }
}

public interface IPlaceItem
{
    public abstract TileTemplate Template { get; }

    protected static void Place(Game game, IDictionary<Item, int> items, Item item)
    {
        if (!game.CurrentTile.Template.IsReplaceable)
            return;
        game.CurrentTile = game.CurrentTile with { Template = ((Torch)item).Template };
        items.RemoveQuantity(item, 1);
        game.Messages.Add(new PlaceMessage(game, item));
        game.CurrentLevel.Update();
    }

    protected class PlaceMessage(Game game, Item item) : Messages.Message(game)
    {
        public override string ToString()
        => $"{_timestamp} {PlayerColor()} placed {ItemColor(item)}";
    }
}

public interface IEatItem
{
    public abstract int RestoreHP { get; }

    protected static void EatAction(Game game, IDictionary<Item, int> items, Item item)
    {
        if (item is not IEatItem eat)
            return;
        var hp = game.Player.HP;
        game.Player.HP += eat.RestoreHP;
        items.RemoveQuantity(item, 1);
        game.Messages.Add(new EatMessage(game, item, game.Player.HP - hp));
    }

    public class EatMessage(Game game, Item item, int hpRestored) : Messages.Message(game)
    {
        public override string ToString()
        => $"{_timestamp} {PlayerColor()} ate {ItemColor(item)} for {hpRestored} ❤️";
    }
}

public interface IEquipItem
{
    public static abstract Creature.EquipmentType EquipmentType { get; }

    protected static void Equip<TItem>(Game game, IDictionary<Item, int> items, Item item)
    where TItem : Item, IEquipItem
    {
        if (!game.Player.Equipments.ContainsKey(TItem.EquipmentType))
        {
            game.Player.Equipments[TItem.EquipmentType] = item;
            game.Messages.Add(new Creature.Player.EquipMessage<TItem>(game, (TItem)item));
            items.RemoveQuantity(item, 1);
            game.CurrentLevel.Update();
        }
    }
}

public abstract record class Torch(string Name, double Range, TileTemplate Template) : Item(Name, Template.Symbol, Template.Foreground), IPlaceItem, IEquipItem
{
    public static Creature.EquipmentType EquipmentType => Creature.EquipmentType.Torch;

    public override IReadOnlyDictionary<string, Action> Actions { get; } = new Dictionary<string, Action>()
    {
        ["Place"] = IPlaceItem.Place,
        ["Equip"] = IEquipItem.Equip<Torch>,
    };
}

public record class SmallTorch() : Torch("Small Torch", 3, TileTemplate.SmallTorch())
{
    public static SmallTorch Instance => field ??= new();
}

public record class Meat() : Item("Meat", '%', Color.IndianRed1), IEatItem
{
    public static Meat Instance => field ??= new();
    public int RestoreHP => 2;

    public override IReadOnlyDictionary<string, Action> Actions { get; } = new Dictionary<string, Action>()
    {
        ["Eat"] = IEatItem.EatAction,
    };

}
