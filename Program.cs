using System.Diagnostics;
using DungeonGenerator;
using DungeonGenerator.Tile;
using Spectre.Console;

var alternateScreen = false;
Console.CursorVisible = false;
AnsiConsole.Console.WriteAnsi(w => w.ExitAltScreen()); // To clear the alternate screen if previously wrongly exited
AnsiConsole.Console.WriteAnsi(w => w.EnterAltScreen().CursorHome()); alternateScreen = true;
Console.CancelKeyPress += (s, e) =>
{
    if (alternateScreen)
    {
        AnsiConsole.Console.WriteAnsi(w => w.ExitAltScreen());
        alternateScreen = false;
    }
    Console.CursorVisible = true;
};

try
{
    var game = new Game(100, 20, 01234);
    while (true)
    {
        var header = $"{game.Steps}:{game.Depth}";
        Console.SetCursorPosition(game.Width - header.Length, 1);
        Console.WriteLine(header);
        for (int y = 0; y < game.Height; y++)
        {
            for (int x = 0; x < game.Width; x++)
                WriteTile(game, x, y);
            Console.WriteLine();
        }
        switch (Console.ReadKey(intercept: true))
        {
            case { Key: ConsoleKey.D1 }:
                _ = (game.MoveDown() && game.MoveLeft()) || (game.MoveLeft() && game.MoveDown());
                break;
            case { Key: ConsoleKey.DownArrow or ConsoleKey.D2 }:
                game.MoveDown();
                break;
            case { Key: ConsoleKey.D3 }:
                _ = (game.MoveDown() && game.MoveRight()) || (game.MoveRight() && game.MoveDown());
                break;
            case { Key: ConsoleKey.LeftArrow or ConsoleKey.D4 }:
                game.MoveLeft();
                break;
            case { Key: ConsoleKey.RightArrow or ConsoleKey.D6 }:
                game.MoveRight();
                break;
            case { Key: ConsoleKey.D7 }:
                _ = (game.MoveUp() && game.MoveLeft()) || (game.MoveLeft() && game.MoveUp());
                break;
            case { Key: ConsoleKey.UpArrow or ConsoleKey.D8 }:
                game.MoveUp();
                break;
            case { Key: ConsoleKey.D9 }:
                _ = (game.MoveUp() && game.MoveRight()) || (game.MoveRight() && game.MoveUp());
                break;
            case { Key: ConsoleKey.Enter or ConsoleKey.Spacebar or ConsoleKey.D5 }:
                game.Activate();
                break;
#if DEBUG
            case { Key: ConsoleKey.T, Modifiers: ConsoleModifiers.Control }
            when game.CurrentLevel[game.Player.X, game.Player.Y].Template is IStairTemplate { Direction: var dir }:
                (game.Player.X, game.Player.Y) = game.CurrentLevel.MainTiles[dir switch
                {
                    IStairTemplate.StairDirection.Up => Level.MainTile.Exit,
                    IStairTemplate.StairDirection.Down => Level.MainTile.Entrance,
                    _ => throw new UnreachableException(),
                }];
                game.CurrentLevel.Update();
                break;
#endif
        }
    }
}
catch (Exception ex) when (!Debugger.IsAttached)
{
    if (alternateScreen)
    {
        AnsiConsole.Console.WriteAnsi(w => w.ExitAltScreen());
        alternateScreen = false;
    }
    AnsiConsole.WriteException(ex);
}
finally
{
    if (alternateScreen)
    {
        Console.ReadLine();
        AnsiConsole.Console.WriteAnsi(w => w.ExitAltScreen());
    }
    Console.CursorVisible = true;
}

static void WriteTile(Game game, int x, int y)
{
    var tile = game.CurrentLevel[x, y];
    var c = tile.Template.Symbol;
    var back = tile.Template.Background;
    var fore = tile.Template.Foreground;
    if ((x, y) == (game.Player.X, game.Player.Y))
    {
        c = game.Player.Symbol;
        fore = game.Player.Color;
    }
    if (!tile.IsVisible)
        if (tile.HasBeenSeen)
            fore = fore.Blend(Color.Black, .75f);
        else
            fore = Color.Black;
    if (fore == back)
        c = '█';
    (AnsiConsole.Background, AnsiConsole.Foreground) = (back, fore);
    AnsiConsole.Write(c);
}
