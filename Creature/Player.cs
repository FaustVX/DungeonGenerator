using DungeonGenerator.Helpers;
using DungeonGenerator.Lib;
using Spectre.Console;

namespace DungeonGenerator.Creature;

public interface IPlayer : ICreature
{
    public abstract double ViewDistance { get; }
    public abstract Direction Direction { get; }
    public abstract IDictionary<Item.Item, int> Items { get; }
}

public record class Player(int X, int Y) : Creature(X, Y, '@', Color.Aqua), IPlayer
{
    public double ViewDistance { get; } = 4;

    public Direction Direction { get; set; }

    public IDictionary<Item.Item, int> Items { get; } = new DefaultableDictionary<Item.Item, int>(capacity: 10, _ => 0);
}
