using DungeonGenerator.Tile;
using Spectre.Console;

namespace DungeonGenerator.Item;

public abstract record class Item(string Name, char Symbol, Color Color);

public interface IActionableItem
{
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
        game.CurrentLevel.DropItem(game.Player.X, game.Player.Y, item, 1);
    }

    protected static void DropAllAction(Game game, Item item)
    {
        var qty = game.Player.Items[item];
        game.Player.Items.Remove(item);
        game.CurrentLevel.DropItem(game.Player.X, game.Player.Y, item, qty);
    }

    protected static void PickupAction(Game game, Item item)
    {
        var qty = game.CurrentLevel.PickupItem(game.Player.X, game.Player.Y, item);
        game.Player.Items[item] += qty;;
    }
}

public abstract record class Torch(string Name, double Range, TileTemplate Template) : Item(Name, Template.Symbol, Template.Foreground), IActionableItem
{
    public IReadOnlyDictionary<string, IActionableItem.Action> Actions { get; } = new Dictionary<string, IActionableItem.Action>()
    {
        ["Place"] = Place,
        ["Equip"] = Equip,
    };

    private static void Equip(Game game, Item item)
    {
        if (item is Torch torch && game.Player.Torch is null)
        {
            game.Player.Torch = torch;
            IActionableItem.DeleteAction(game, torch);
            game.CurrentLevel.Update();
        }
    }

    private static void Place(Game game, Item item)
    {
        if (!game.CurrentTile.Template.IsReplaceable)
            return;
        game.CurrentTile = game.CurrentTile with { Template = ((Torch)item).Template };
        IActionableItem.DeleteAction(game, item);
        game.CurrentLevel.Update();
    }
}

public record class SmallTorch() : Torch("Small Torch", 3, TileTemplate.SmallTorch())
{
    public static SmallTorch Instance => field ??= new();
}
