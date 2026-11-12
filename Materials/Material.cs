using Spectre.Console;
using DungeonGenerator.Helpers;

namespace DungeonGenerator.Materials;

public sealed record class Material(string Name, Color Color)
{
    public static Material Wood { get; } = new(nameof(Wood), Color.Wood);
    public static Material Stone { get; } = new(nameof(Stone), Color.Stone);
    public static Material Iron { get; } = new(nameof(Iron), Color.Iron);
}
