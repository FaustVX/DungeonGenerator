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

    extension((int x, int y) pos)
    {
        public int DistanceSquared(int x, int y)
        {
            var (dx, dy) = (x - pos.x, y - pos.y);
            return dx * dx + dy * dy;
        }

        public double Distance(int x, int y)
        => Math.Sqrt(DistanceSquared(pos, x, y));
    }

    extension(Math)
    {
        public static T Max<T>(params ReadOnlySpan<T> values)
        => values.Max()!;
        public static T Min<T>(params ReadOnlySpan<T> values)
        => values.Min()!;
    }

    public static IEnumerable<(int x, int y)> Line(int x0, int y0, int x1, int y1)
    {
        var dx = Math.Abs(x1 - x0);
        var dy = Math.Abs(y1 - y0);
        var sx = x0 < x1 ? 1 : -1;
        var sy = y0 < y1 ? 1 : -1;
        var err = dx - dy;

        while (true)
        {
            yield return (x0, y0);

            if (x0 == x1 && y0 == y1)
                yield break;

            var e2 = 2 * err;

            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }

            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }
}
