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
        public static void Write(Tile.Tile tile)
        {
            AnsiConsole.Cursor.SetPosition(tile.X, tile.Y);
            AnsiConsole.Background = tile.Template.Background;
            AnsiConsole.Write(tile.Template.Symbol);
        }
    }

    extension(Random rng)
    {
        public bool NextBool(double probality = .5)
        => rng.NextDouble() < probality;
    }
}
