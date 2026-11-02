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

public record class SmallTorch() : Item("Small Torch"), IActionableItem
{
    public static SmallTorch Instance => field ??= new();

    public IReadOnlyDictionary<string, IActionableItem.Action> Actions { get; } = new Dictionary<string, IActionableItem.Action>()
    {
        ["Place"] = Place,
    };
    private static void Place(Game game, Item item)
    {
        game.CurrentLevel[game.Player.X, game.Player.Y] = game.CurrentLevel[game.Player.X, game.Player.Y] with { Template = Tile.TileTemplate.SmallTorch };
        IActionableItem.DeleteAction(game, item);
        game.CurrentLevel.Update();
    }
}
