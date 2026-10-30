using DungeonGenerator;
using Spectre.Console;

var alternateScreen = false;
Console.CursorVisible = false;
AnsiConsole.Console.WriteAnsi(w => w.EnterAltScreen().CursorHome()); alternateScreen = true;

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
            case { Key: ConsoleKey.UpArrow or ConsoleKey.NumPad8 }:
                game.MoveUp();
                break;
            case { Key: ConsoleKey.LeftArrow or ConsoleKey.NumPad4 }:
                game.MoveLeft();
                break;
            case { Key: ConsoleKey.RightArrow or ConsoleKey.NumPad6 }:
                game.MoveRight();
                break;
            case { Key: ConsoleKey.DownArrow or ConsoleKey.NumPad2 }:
                game.MoveDown();
                break;
        }
    }
}
catch (Exception ex)
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
