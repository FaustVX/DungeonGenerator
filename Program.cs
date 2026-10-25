using DungeonGenerator;
using DungeonGenerator.Tile;
using DungeonGenerator.Generator;
using Spectre.Console;

var gen = new BSP(10, 10, (3,3), (5, 5), 0, 1234);
AnsiConsole.Write(new Tile(5, 5, TileTemplate.Window, true));
