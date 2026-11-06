using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using Spectre.Console;

namespace DungeonGenerator.Helpers;

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

    extension(AnsiWriter writer)
    {
        public AnsiWriter EnableMouse()
        => writer.WriteCsi(1000, 'h', decPrivateMode: true) // enable mouse
            .WriteCsi(1003, 'h', decPrivateMode: true) // enable mouse motion
            .WriteCsi(1006, 'h', decPrivateMode: true); // enable extended
        public AnsiWriter DisableMouse()
        => writer.WriteCsi(1000, 'l', decPrivateMode: true);

        [UnsafeAccessor(UnsafeAccessorKind.Method)]
        public extern AnsiWriter WriteCsi(int value, char terminator, bool decPrivateMode = false);
        [UnsafeAccessor(UnsafeAccessorKind.Method)]
        public extern AnsiWriter WriteCsi(string parameter, char terminator, bool decPrivateMode = false);
    }

    extension(IAnsiConsoleInput input)
    {
        /// <returns>Returns either a <see cref="ConsoleKeyInfo"/> or <see cref="ConsoleRead.MouseEvent"/></returns>
        /// <inheritdoc cref="IAnsiConsoleInput.ReadKey(bool)"/>>
        public ConsoleRead ReadKeyOrMouse()
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key is not ConsoleKey.Escape || !Console.KeyAvailable)
                return key;
            if (Console.ReadKey(true).KeyChar != '[' || Console.ReadKey(true).KeyChar != '<')
            {
                while (Console.KeyAvailable)
                    Console.ReadKey(true);
                return new ConsoleKeyInfo();
            }

            var button = 0;
            while ((key = Console.ReadKey(true)).KeyChar is not ';')
                button = button * 10 + (key.KeyChar - '0');
            button &= ~(0b111 << 2);
            var x = 0;
            while ((key = Console.ReadKey(true)).KeyChar is not ';')
                x = x * 10 + (key.KeyChar - '0');
            var y = 0;
            while ((key = Console.ReadKey(true)).KeyChar is not ('M' or 'm'))
                y = y * 10 + (key.KeyChar - '0');
            var pressed = key.KeyChar is 'M';
            Convert.ToString(button, 2);
            return new ConsoleRead.MouseEvent((ConsoleRead.MouseButton)button, x - 1, y - 1, pressed);
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

    extension<T>(SelectionPrompt<T>)
    where T : notnull
    {
        public static SelectionPrompt<T> Default(string title, Func<T, string> converter, params IEnumerable<T> values)
        => new SelectionPrompt<T>()
            .AddChoices([..values])
            .EnableSearch()
            .WrapAround()
            .UseConverter(converter)
            .Title($"{title}:");

        public static SelectionPrompt<T> Default(string title, Func<T, string> converter, T cancel, params IEnumerable<T> values)
        => Default(title, converter, values)
            .AddCancelResult(cancel);
    }
}
