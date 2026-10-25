using DungeonGenerator;
using Spectre.Console;

var game = new Game(1234);
for (int y = 0; y < game.Height; y++)
{
    for (int x = 0; x < game.Width; x++)
        AnsiConsole.Write(game.CurrentLevel[x, y].Template);
    Console.WriteLine();
}
