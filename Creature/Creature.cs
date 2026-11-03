using Spectre.Console;

namespace DungeonGenerator.Creature;

public interface ICreature
{
    public abstract int X { get; set; }
    public abstract int Y { get; set; }
    public abstract char Symbol { get; }
    public abstract Color Color { get; }
    public abstract int HP { get; set; }
    public abstract int MaxHP { get; }
    public abstract int Shields { get; set; }
}

public record class Creature(int X, int Y, char Symbol, int HP, Color Color) : ICreature
{
    public int X { get; set; } = X;
    public int Y { get; set; } = Y;
    public int HP { get; set => field = Math.Clamp(value, 0, MaxHP); } = HP;
    public int Shields { get; set => field = Math.Max(value, 0); }
    public int MaxHP { get; } = HP;
}
