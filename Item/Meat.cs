using Spectre.Console;

namespace DungeonGenerator.Item;

public record class Meat() : Item("Meat", '%', Color.IndianRed1), IEatItem
{
    public static Meat Instance => field ??= new();
    public int RestoreHP => 2;

    public override IReadOnlyDictionary<string, Action> Actions { get; } = new Dictionary<string, Action>()
    {
        ["Eat"] = IEatItem.EatAction,
    };

}
