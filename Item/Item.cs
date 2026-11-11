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
