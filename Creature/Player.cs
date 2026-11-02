using DungeonGenerator.Helpers;
using DungeonGenerator.Lib;
using Spectre.Console;

namespace DungeonGenerator.Creature;

public record class Player(int X, int Y) : Creature(X, Y, '@', Color.Aqua)
{
    public double ViewDistance => Torch?.Range ?? 1;
    public Direction Direction { get; set; }
    public Item.Torch? Torch { get; set; }

    public IDictionary<Item.Item, int> Items { get; } = new DefaultableDictionary<Item.Item, int>(capacity: 10, _ => 0);
}
