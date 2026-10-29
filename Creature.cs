using Spectre.Console;

namespace DungeonGenerator;

public interface ICreature
{
    public abstract int X { get; }
    public abstract int Y { get; }
    public abstract char Symbol { get; }
    public abstract Color Color { get; }
}

public interface IPlayer : ICreature;

public record class Creature(int X, int Y, char Symbol, Color Color) : ICreature
{
    public int X { get; set; } = X;
    public int Y { get; set; } = Y;
}

public record class Player(int X, int Y) : Creature(X, Y, '@', Color.Aqua), IPlayer;
