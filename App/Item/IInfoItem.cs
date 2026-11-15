namespace DungeonGenerator.App.Item;

[InitItem]
public interface IInfoItem
{
    protected static abstract IEnumerable<InfoMessage> GetInfo(Game game, Item item);

    private static void InfoAction<T>(Game game, IDictionary<Item, int> items, Item item)
    where T : Item, IInfoItem
    => game.Messages.AddRange(T.GetInfo(game, item));

    protected class InfoMessage(Game game, Item item, string message) : Messages.Message(game, $"{ItemColor(item)}: {message}");

    protected static void Init<T>()
    where T : Item, IInfoItem
    {
        Item._actionsCache[(typeof(T), ItemLocation.Ground)].Add("info", InfoAction<T>);
        Item._actionsCache[(typeof(T), ItemLocation.Inventory)].Add("info", InfoAction<T>);
        Item._actionsCache[(typeof(T), ItemLocation.Equipment)].Add("info", InfoAction<T>);
    }
}
