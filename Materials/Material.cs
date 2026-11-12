using Spectre.Console;
using DungeonGenerator.Helpers;

namespace DungeonGenerator.Materials;

public sealed record class Material(string Name, Color Color, double Hardness)
{
    public static Material Wood { get; } = new(nameof(Wood), Color.Wood, .5);
    public static Material Stone { get; } = new(nameof(Stone), Color.Stone, .75);
    public static Material Iron { get; } = new(nameof(Iron), Color.Iron, 1);
}
