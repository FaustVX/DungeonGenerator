using DungeonGenerator.Tile;

namespace DungeonGenerator.Item;

public abstract record class Item(string Name);

public interface IActionableItem
{
    public delegate void Action(Game game, Item item);
    public abstract IReadOnlyDictionary<string, Action> Actions { get; }
    public static KeyValuePair<string, Action> Cancel { get; } = new("Cancel", delegate{});
    public static KeyValuePair<string, Action> Delete { get; } = new("Delete", DeleteAction);
    protected static void DeleteAction(Game game, Item item)
    {
        if ((game.Player.Items[item] -= 1) <= 0)
            game.Player.Items.Remove(item);
    }
}

public abstract record class Torch(string Name, double Range, TileTemplate Template) : Item(Name), IActionableItem
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
        if (!game.CurrentLevel[game.Player.X, game.Player.Y].Template.IsReplaceable)
            return;
        game.CurrentLevel[game.Player.X, game.Player.Y] = game.CurrentLevel[game.Player.X, game.Player.Y] with { Template = ((Torch)item).Template };
        IActionableItem.DeleteAction(game, item);
        game.CurrentLevel.Update();
    }
}

public record class SmallTorch() : Torch("Small Torch", 3, TileTemplate.SmallTorch())
{
    public static SmallTorch Instance => field ??= new();
}
