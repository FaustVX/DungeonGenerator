using System.Diagnostics;
using System.Numerics;
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

        public T NextItem<T>(IReadOnlyList<T> values)
        => values[rng.Next(values.Count)];
    }

    extension<T>((T, T))
    where T : IAdditionOperators<T, T, T>
    {
        public static (T, T) operator +((T, T) lhs, (T, T) rhs)
        => (lhs.Item1 + rhs.Item1, lhs.Item2 + rhs.Item2);
    }

    extension<T>((T, T))
    where T : ISubtractionOperators<T, T, T>
    {
        public static (T, T) operator -((T, T) lhs, (T, T) rhs)
        => (lhs.Item1 - rhs.Item1, lhs.Item2 - rhs.Item2);
    }
}
