using Spectre.Console;

namespace DungeonGenerator.App.Item;

public partial record class Meat() : Item("Meat", '%', Color.IndianRed1), IEatItem, IInfoItem
{
    public static Meat Instance => field ??= new();
    public int RestoreHP => 2;

    static IEnumerable<IInfoItem.InfoMessage> IInfoItem.GetInfo(Game game, Item item)
    => [new(game, item, IEatItem.GetInfo(item))];
}
