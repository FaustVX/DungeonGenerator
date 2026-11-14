using DungeonGenerator.Helpers;
using Spectre.Console;

namespace DungeonGenerator.Item;

public interface IEatItem
{
    public abstract int RestoreHP { get; }

    protected static void EatAction(Game game, IDictionary<Item, int> items, Item item)
    {
        if (item is not IEatItem eat)
            return;
        var hp = game.Player.HP;
        game.Player.HP += eat.RestoreHP;
        items.RemoveQuantity(item, 1);
        game.Messages.Add(new EatMessage(game, item, game.Player.HP - hp));
    }

    public class EatMessage(Game game, Item item, int hpRestored) : Messages.Message(game)
    {
        public override string ToString()
        => $"{_timestamp} {PlayerColor()} ate {ItemColor(item)} for {WithColor(hpRestored, Color.Red)} ❤️";
    }

    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Init()
    => Item.Init(typeof(IEatItem));

    protected static void Init<T>()
    where T : Item, IEatItem
    {
        Item._actionsCache[(typeof(T), ItemLocation.Ground)].Add("Eat", EatAction);
        Item._actionsCache[(typeof(T), ItemLocation.Inventory)].Add("Eat", EatAction);
    }
}
