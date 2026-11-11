using DungeonGenerator.Helpers;
using DungeonGenerator.Tile;

namespace DungeonGenerator.Item;

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
        game.CurrentLevel.UpdateLight();
    }

    protected class PlaceMessage(Game game, Item item) : Messages.Message(game)
    {
        public override string ToString()
        => $"{_timestamp} {PlayerColor()} placed {ItemColor(item)}";
    }
}
