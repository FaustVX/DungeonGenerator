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

    public class EatMessage(Game game, Item item, int hpRestored) : Messages.Message(game)
    {
        public override string ToString()
        => $"{_timestamp} {PlayerColor()} ate {ItemColor(item)} for {hpRestored} ❤️";
    }
    public delegate void Action(Game game, Item item);
    public abstract IReadOnlyDictionary<string, Action> Actions { get; }
    public static KeyValuePair<string, Action> Cancel { get; } = new("Cancel", delegate{});
    public static KeyValuePair<string, Action> Delete { get; } = new("Delete", DeleteAction);
    public static KeyValuePair<string, Action> Drop1 { get; } = new("Drop 1", Drop1Action);
    public static KeyValuePair<string, Action> DropAll { get; } = new("Drop all", DropAllAction);
    public static KeyValuePair<string, Action> Pickup { get; } = new("Pickup", PickupAction);

    protected static void DeleteAction(Game game, Item item)
    {
        if ((game.Player.Items[item] -= 1) <= 0)
            game.Player.Items.Remove(item);
    }

    protected static void Drop1Action(Game game, Item item)
    {
        if ((game.Player.Items[item] -= 1) <= 0)
            game.Player.Items.Remove(item);
        game.CurrentLevel.DropItem(game.Player.X, game.Player.Y, item, 1, game.Player);
    }

    protected static void DropAllAction(Game game, Item item)
    {
        var qty = game.Player.Items[item];
        game.Player.Items.Remove(item);
        game.CurrentLevel.DropItem(game.Player.X, game.Player.Y, item, qty, game.Player);
    }

    protected static void PickupAction(Game game, Item item)
    {
        var qty = game.CurrentLevel.PickupItem(game.Player.X, game.Player.Y, item);
        game.Player.Items[item] += qty;
        game.Messages.Add(new PickupMessage(game, game.Player, qty, item));
    }
}

public interface IPlaceItem
{
    public abstract TileTemplate Template { get; }

    protected static void Place(Game game, Item item)
    {
        if (!game.CurrentTile.Template.IsReplaceable)
            return;
        game.CurrentTile = game.CurrentTile with { Template = ((Torch)item).Template };
        if ((game.Player.Items[item] -= 1) <= 0)
            game.Player.Items.Remove(item);
        game.Messages.Add(new PlaceMessage(game, item));
        game.CurrentLevel.Update();
    }

    protected class PlaceMessage(Game game, Item item) : Messages.Message(game)
    {
        public override string ToString()
        => $"{_timestamp} {PlayerColor()} placed {ItemColor(item)}";
    }
}

public abstract record class Torch(string Name, double Range, TileTemplate Template) : Item(Name, Template.Symbol, Template.Foreground), IPlaceItem
{
    public override IReadOnlyDictionary<string, Action> Actions { get; } = new Dictionary<string, Action>()
    {
        ["Place"] = IPlaceItem.Place,
        ["Equip"] = Equip,
    };

    private static void Equip(Game game, Item item)
    {
        if (item is Torch torch && game.Player.Torch is null)
        {
            game.Player.Torch = torch;
            DeleteAction(game, torch);
            game.CurrentLevel.Update();
        }
    }
}

public record class SmallTorch() : Torch("Small Torch", 3, TileTemplate.SmallTorch())
{
    public static SmallTorch Instance => field ??= new();
}

public record class Meat() : Item("Meat", '%', Color.IndianRed1)
{
    public static Meat Instance => field ??= new();

    public override IReadOnlyDictionary<string, Action> Actions { get; } = new Dictionary<string, Action>()
    {
        ["Eat"] = EatAction,
    };

    private static void EatAction(Game game, Item item)
    {
        var hp = game.Player.HP;
        game.Player.HP += 2;
        if ((game.Player.Items[item] -= 1) <= 0)
            game.Player.Items.Remove(item);
        game.Messages.Add(new EatMessage(game, item, game.Player.HP - hp));
    }
}
