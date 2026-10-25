using System.Diagnostics;
using System.Runtime.CompilerServices;
using Spectre.Console;

namespace DungeonGenerator;

static class Ext
{
    extension(ArgumentOutOfRangeException)
    {
        [DebuggerStepThrough]
        public static void ThrowIfNotInBetween<T>(T value, T min, T max, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : IComparable<T>
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, max, paramName);
            ArgumentOutOfRangeException.ThrowIfLessThan(value, min, paramName);
        }
    }

    extension(AnsiConsole)
    {
        public static void Write(Tile.TileTemplate template)
        {
            AnsiConsole.Background = template.Background;
            AnsiConsole.Write(template.Symbol);
        }
    }

    extension(Random rng)
    {
        public bool NextBool(double probality = .5)
        => rng.NextDouble() < probality;
    }
}
