using DungeonGenerator;
using DungeonGenerator.Tile;
using DungeonGenerator.Generator;
using Spectre.Console;

var gen = new BSP(80, 25, (3,3), (5, 5), 0, 1234);
for (int y = 0; y < gen.Height; y++)
{
    for (int x = 0; x < gen.Width; x++)
        AnsiConsole.Write(gen.GetTile(x, y));
    Console.WriteLine();
}
