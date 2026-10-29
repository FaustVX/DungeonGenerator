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
        Console.SetCursorPosition(0, 1);
        for (int y = 0; y < game.Height; y++)
        {
            for (int x = 0; x < game.Width; x++)
            {
                var template = game.CurrentLevel[x, y].Template;
                if ((game.Player.X, game.Player.Y) == (x, y))
                {
                    (AnsiConsole.Background, AnsiConsole.Foreground) = (template.Background, game.Player.Color);
                    AnsiConsole.Write(game.Player.Symbol);
                    AnsiConsole.Reset();
                }
                else
                    AnsiConsole.Write(template);
            }
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
