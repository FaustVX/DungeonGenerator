using DungeonGenerator.Helpers;
using DungeonGenerator.Lib;
using Spectre.Console;

namespace DungeonGenerator.Item;

public abstract record class Item(string Name, char Symbol, Color Color)
{
    public static readonly DefaultableDictionary<(Type itemType, ItemLocation itemLocation), Dictionary<string, Action>> _actionsCache = [with(_ => [])];

    public class DropMessage(Game game, Creature.Creature creature, int qty, Item item) : Messages.Message(game)
    {
        public override string ToString()
        => $"{_timestamp} {CreatureColor(creature)} dropped {WithColor(qty, Color.Blue)} {ItemColor(item)} on the ground";
    }

    public class PickupMessage(Game game, Creature.Creature creature, int qty, Item item) : Messages.Message(game)
    {
        public override string ToString()
        => $"{_timestamp} {CreatureColor(creature)} picked up {WithColor(qty, Color.Blue)} {ItemColor(item)} from the ground";
    }

    public virtual string Name { get; } = Name;

    public delegate void Action(Game game, IDictionary<Item, int> items, Item item);
    public IReadOnlyDictionary<string, Action> Actions(ItemLocation location)
    => _actionsCache.TryGetValue((GetType(), location), out var actions) ? actions : [];
    public static KeyValuePair<string, Action> Cancel { get; } = new("Cancel", delegate{});

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

    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Init()
#pragma warning disable CA2263 // Prefer generic overload when type is known
    => Init(typeof(Item));
#pragma warning restore CA2263 // Prefer generic overload when type is known

    protected static void Init<T>()
    where T : Item
    {
        _actionsCache[(typeof(T), ItemLocation.Ground)].Add("Pickup", PickupAction);
        _actionsCache[(typeof(T), ItemLocation.Ground)].Add("Delete", DeleteAction);
        _actionsCache[(typeof(T), ItemLocation.Inventory)].Add("Delete", DeleteAction);
        _actionsCache[(typeof(T), ItemLocation.Equipment)].Add("Delete", DeleteAction);
        _actionsCache[(typeof(T), ItemLocation.Inventory)].Add("Drop 1", Drop1Action);
        _actionsCache[(typeof(T), ItemLocation.Inventory)].Add("Drop all", DropAllAction);
        _actionsCache[(typeof(T), ItemLocation.Ground)].Add("Cancel", delegate{});
        _actionsCache[(typeof(T), ItemLocation.Inventory)].Add("Cancel", delegate{});
        _actionsCache[(typeof(T), ItemLocation.Equipment)].Add("Cancel", delegate{});
    }

    public static void Init(Type initType, string initName = "Init")
    {
        var init = initType
            .GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
            .Single(method => method.Name == initName && method.IsGenericMethodDefinition);

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            foreach (var type in assembly.GetTypes())
            {
                if (type.IsClass && !type.IsAbstract && !type.ContainsGenericParameters &&
                    typeof(Item).IsAssignableFrom(type) && initType.IsAssignableFrom(type))
                    init.MakeGenericMethod(type).Invoke(null, null);
            }
    }
}

public enum ItemLocation
{
    Ground,
    Inventory,
    Equipment,
}
