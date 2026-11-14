using Spectre.Console;

namespace DungeonGenerator.Item;

public record class Meat() : Item("Meat", '%', Color.IndianRed1), IEatItem
{
    public static Meat Instance => field ??= new();
    public int RestoreHP => 2;
}
