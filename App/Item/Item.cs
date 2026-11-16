using DungeonGenerator.App.Helpers;
using DungeonGenerator.App.Lib;
using Spectre.Console;

namespace DungeonGenerator.App.Item;

public abstract record class Item(string Name, char Symbol, Color Color)
{
    public static readonly DefaultableDictionary<(Type itemType, ItemLocation itemLocation), Dictionary<string, Action>> _actionsCache = [with(_ => [])];

    public class DropMessage(Game game, Creature.Creature creature, int qty, Item item)
    : Messages.Message(game, $"{CreatureColor(creature)} dropped {WithColor(qty, Color.Blue)} {ItemColor(item)} on the ground");

    public class PickupMessage(Game game, Creature.Creature creature, int qty, Item item)
    : Messages.Message(game, $"{CreatureColor(creature)} picked up {WithColor(qty, Color.Blue)} {ItemColor(item)} from the ground");

    public virtual string Name { get; } = Name;

    public delegate void Action(Game game, IDictionary<Item, int> items, Item item);
    public IReadOnlyDictionary<string, Action> Actions(ItemLocation location)
    => _actionsCache.TryGetValue((GetType(), location), out var actions) ? actions : [];
    public static KeyValuePair<string, Action> Cancel { get; } = new("cancel", delegate{});

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
        game.CurrentLevel.Update();
    }

    protected static void Init<T>()
    where T : Item
    {
        _actionsCache[(typeof(T), ItemLocation.Ground)].Add("pickup", PickupAction);
        _actionsCache[(typeof(T), ItemLocation.Inventory)].Add("drop 1", Drop1Action);
        _actionsCache[(typeof(T), ItemLocation.Inventory)].Add("drop all", DropAllAction);
        _actionsCache[(typeof(T), ItemLocation.Ground)].Add("delete", DeleteAction);
        _actionsCache[(typeof(T), ItemLocation.Inventory)].Add("delete", DeleteAction);
        _actionsCache[(typeof(T), ItemLocation.Equipment)].Add("delete", DeleteAction);
        _actionsCache[(typeof(T), ItemLocation.Ground)].Add("cancel", delegate{});
        _actionsCache[(typeof(T), ItemLocation.Inventory)].Add("cancel", delegate{});
        _actionsCache[(typeof(T), ItemLocation.Equipment)].Add("cancel", delegate{});
    }
}

public enum ItemLocation
{
    Ground,
    Inventory,
    Equipment,
}
