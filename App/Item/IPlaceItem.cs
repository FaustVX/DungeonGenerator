using DungeonGenerator.App.Helpers;
using DungeonGenerator.App.Tile;

namespace DungeonGenerator.App.Item;

[InitItem]
public interface IPlaceItem
{
    public abstract TileTemplate Template { get; }
    protected static string GetInfo<T>(Item item, string header, Func<TileTemplate, T> selector)
    => $"{header}: {selector(((IPlaceItem)item).Template)}";

    protected static void Place(Game game, IDictionary<Item, int> items, Item item)
    {
        if (!game.CurrentTile.Template.IsReplaceable)
            return;
        game.CurrentTile = game.CurrentTile with { Template = ((Torch)item).Template };
        items.RemoveQuantity(item, 1);
        game.Messages.Add(new PlaceMessage(game, item));
        game.CurrentLevel.UpdateLight();
    }

    protected class PlaceMessage(Game game, Item item)
    : Messages.Message(game, $"{CreatureColor(game.Player)} placed {ItemColor(item)}");

    protected static void Init<T>()
    where T : Item, IPlaceItem
    {
        Item._actionsCache[(typeof(T), ItemLocation.Ground)].Add("place", Place);
        Item._actionsCache[(typeof(T), ItemLocation.Inventory)].Add("place", Place);
    }
}
