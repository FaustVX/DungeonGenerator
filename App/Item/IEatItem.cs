using DungeonGenerator.App.Helpers;
using Spectre.Console;

namespace DungeonGenerator.App.Item;

[InitItem, InfoItem]
public interface IEatItem
{
    public abstract int RestoreHP { get; }
    protected static string GetInfo(Item item)
    => $"Restore HP: {((IEatItem)item).RestoreHP}";

    protected static void EatAction(Game game, IDictionary<Item, int> items, Item item)
    {
        if (item is not IEatItem eat)
            return;
        var hp = game.Player.HP;
        game.Player.HP += eat.RestoreHP;
        items.RemoveQuantity(item, 1);
        game.Messages.Add(new EatMessage(game, item, game.Player.HP - hp));
    }

    public class EatMessage(Game game, Item item, int hpRestored)
    : Messages.Message(game, $"{CreatureColor(game.Player)} ate {ItemColor(item)} for {WithColor(hpRestored, Color.Red)} ❤️");

    protected static void Init<T>()
    where T : Item, IEatItem
    {
        Item._actionsCache[(typeof(T), ItemLocation.Ground)].Add("eat", EatAction);
        Item._actionsCache[(typeof(T), ItemLocation.Inventory)].Add("eat", EatAction);
    }
}
