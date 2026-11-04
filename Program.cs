using System.Diagnostics;
using DungeonGenerator;
using DungeonGenerator.Item;
using DungeonGenerator.Tile;
using Spectre.Console;

var alternateScreen = false;
Console.CursorVisible = false;
AnsiConsole.Console.WriteAnsi(w => w.ExitAltScreen()); // To clear the alternate screen if previously wrongly exited
AnsiConsole.Console.WriteAnsi(w => w.EnterAltScreen().CursorHome()); alternateScreen = true;
Console.CancelKeyPress += (s, e) => Exit();

try
{
    var game = new Game(100, 20, args is [var arg0]
        ? int.TryParse(arg0, out var seed) ? seed: arg0.GetHashCode()
        : Random.Shared.Next());
    Run: while (true)
    {
        DrawDungeon(game);
        switch (Console.ReadKey(intercept: true))
        {
            case { Key: ConsoleKey.Escape }:
                var exit = AnsiConsole.Confirm("Quit", defaultValue: false);
                Console.Clear();
                if (exit)
                    break Run;
                break;
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
            case { Key: ConsoleKey.E }:
            {
                if (game.Player.Items.Count <= 0)
                    break;
                var promptItem = new SelectionPrompt<KeyValuePair<Item, int>>()
                .AddChoices(game.Player.Items)
                .AddCancelResult(new KeyValuePair<Item, int>(default!, 0))
                .EnableSearch()
                .WrapAround()
                .UseConverter(kvp => $"{kvp.Key.Name}: {kvp.Value}")
                .Title("Inventory:");
                var (item, qty) = AnsiConsole.Prompt(promptItem);
                DrawDungeon(game);
                if (qty == 0)
                {
                    AnsiConsole.Cursor.Hide();
                    break;
                }

                var promptAction = new SelectionPrompt<KeyValuePair<string, IActionableItem.Action>>()
                .AddChoices([..item is IActionableItem i ? i.Actions : Enumerable.Empty<KeyValuePair<string, IActionableItem.Action>>(), IActionableItem.Drop1, IActionableItem.DropAll, IActionableItem.Delete, IActionableItem.Cancel])
                .AddCancelResult(IActionableItem.Cancel)
                .EnableSearch()
                .WrapAround()
                .UseConverter(kvp => kvp.Key)
                .Title(item.Name);
                AnsiConsole.Prompt(promptAction).Value(game, item);
                AnsiConsole.Cursor.Hide();
                break;
            }
            case { Key: ConsoleKey.Enter } when game.CurrentLevel[game.Player.X, game.Player.Y] is { Items: { Count: > 0 } items }:
            {
                var promptItem = new SelectionPrompt<KeyValuePair<Item, int>>()
                .AddChoices(items)
                .AddCancelResult(new KeyValuePair<Item, int>(default!, 0))
                .EnableSearch()
                .WrapAround()
                .UseConverter(kvp => $"{kvp.Key.Name}: {kvp.Value}")
                .Title("Ground:");
                var (item, qty) = AnsiConsole.Prompt(promptItem);
                DrawDungeon(game);
                if (qty == 0)
                {
                    AnsiConsole.Cursor.Hide();
                    break;
                }

                var promptAction = new SelectionPrompt<KeyValuePair<string, IActionableItem.Action>>()
                .AddChoices([IActionableItem.Pickup, IActionableItem.Cancel])
                .AddCancelResult(IActionableItem.Cancel)
                .EnableSearch()
                .WrapAround()
                .UseConverter(kvp => kvp.Key)
                .Title(item.Name);
                AnsiConsole.Prompt(promptAction).Value(game, item);
                AnsiConsole.Cursor.Hide();
                break;
            }
            case { Key: ConsoleKey.Enter } when game.CurrentLevel[game.Player.X, game.Player.Y].Template is IActionableTemplate { Actions: { } actions, MenuTitle: var title }:
            {
                var cancel = IActionableTemplate.Cancel;
                var prompt = new SelectionPrompt<KeyValuePair<string, IActionableTemplate.Action>>()
                .AddChoices(actions.Append(cancel))
                .EnableSearch()
                .WrapAround()
                .UseConverter(kvp => kvp.Key)
                .Title($"{title}:")
                .AddCancelResult(cancel);
                AnsiConsole.Prompt(prompt).Value(game, game.Player.X, game.Player.Y);
                AnsiConsole.Cursor.Hide();
                break;
            }
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
    Exit();
    AnsiConsole.WriteException(ex);
}
finally
{
    Exit(wait: true);
}

void Exit(bool wait = false)
{
    if (alternateScreen)
    {
        if (wait)
        {
            AnsiConsole.Write("Press any key to exit");
            Console.ReadLine();
        }
        AnsiConsole.Console.WriteAnsi(w => w.ExitAltScreen());
        alternateScreen = false;
    }
    AnsiConsole.Cursor.Show();
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
    else if (tile.Items.Count >= 1)
        if (tile.Items.Count == 1)
        {
            var item = tile.Items.First().Key;
            c = item.Symbol;
            fore = item.Color;
        }
        else
            c = '+';

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

static void DrawDungeon(Game game)
{
    var header = $"{game.Steps}:{game.Depth}";
    AnsiConsole.Cursor.MoveRight(AnsiConsole.Profile.Width - game.Width - 1);
    PrintStat(game.Player.HP, "❤️");
    Console.Write(' ');
    PrintStat(game.Player.Shields, "🛡");
    Console.SetCursorPosition(AnsiConsole.Profile.Width - header.Length - 1, 1);
    Console.WriteLine(header);
    for (int y = 0; y < game.Height; y++)
    {
        AnsiConsole.Cursor.MoveRight(AnsiConsole.Profile.Width - game.Width - 1);
        for (int x = 0; x < game.Width; x++)
            WriteTile(game, x, y);
        Console.WriteLine();
    }
    AnsiConsole.Cursor.SetPosition(0, 1);

    static void PrintStat(int stat, string text)
    {
        if (stat >= 10)
            Console.Write($"{text} :{stat}");
        else
            for (int i = 0; i < stat; i++)
                Console.Write(text);
    }
}
