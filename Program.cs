using System.Diagnostics;
using DungeonGenerator;
using DungeonGenerator.Helpers;
using DungeonGenerator.Item;
using DungeonGenerator.Tile;
using Spectre.Console;

MouseSupport(true);

var alternateScreen = false;
Console.CursorVisible = false;
AnsiConsole.Console.WriteAnsi(w => w.ExitAltScreen()); // To clear the alternate screen if previously wrongly exited
AnsiConsole.Console.WriteAnsi(w => w.EnterAltScreen().CursorHome()); alternateScreen = true;
Console.CancelKeyPress += (s, e) => Exit();

try
{
    var mousePos = (x: 0, y: 0);
    var game = new Game(100, 20, args is [var arg0]
        ? int.TryParse(arg0, out var seed) ? seed: arg0.GetHashCode()
        : Random.Shared.Next());
    Run: while (true)
    {
        var lookingAt = LookingAt(game, mousePos.x, mousePos.y);
        DrawDungeon(game, lookingAt);
        switch (AnsiConsole.Console.Input.ReadKeyOrMouse())
        {
            case ConsoleKeyInfo { Key: ConsoleKey.Escape }:
                MouseSupport(false);
                var exit = AnsiConsole.Confirm("Quit", defaultValue: false);
                Console.Clear();
                if (exit)
                    break Run;
                MouseSupport(true);
                break;
            case ConsoleRead.MouseEvent { PressedOrMovement: true, Button: ConsoleRead.MouseButton.Movement } mouse:
            {
                if (mouse.X - (Console.WindowWidth - game.Width) is var x and >= 0 && mouse.Y - 2 is var y and >= 0 && y < game.Height )
                    mousePos = (x, y);
                break;
            }
            case ConsoleKeyInfo { Key: ConsoleKey.D1 }:
                _ = (game.MoveDown() && game.MoveLeft()) || (game.MoveLeft() && game.MoveDown());
                break;
            case ConsoleKeyInfo { Key: ConsoleKey.DownArrow or ConsoleKey.D2 }:
                game.MoveDown();
                break;
            case ConsoleKeyInfo { Key: ConsoleKey.D3 }:
                _ = (game.MoveDown() && game.MoveRight()) || (game.MoveRight() && game.MoveDown());
                break;
            case ConsoleKeyInfo { Key: ConsoleKey.LeftArrow or ConsoleKey.D4 }:
                game.MoveLeft();
                break;
            case ConsoleKeyInfo { Key: ConsoleKey.RightArrow or ConsoleKey.D6 }:
                game.MoveRight();
                break;
            case ConsoleKeyInfo { Key: ConsoleKey.D7 }:
                _ = (game.MoveUp() && game.MoveLeft()) || (game.MoveLeft() && game.MoveUp());
                break;
            case ConsoleKeyInfo { Key: ConsoleKey.UpArrow or ConsoleKey.D8 }:
                game.MoveUp();
                break;
            case ConsoleKeyInfo { Key: ConsoleKey.D9 }:
                _ = (game.MoveUp() && game.MoveRight()) || (game.MoveRight() && game.MoveUp());
                break;
            case ConsoleKeyInfo { Key: ConsoleKey.E }:
            {
                if (game.Player.Items is not IReadOnlyDictionary<Item, int> { Count: > 0 } items)
                    break;
                ItemsMenu(game, lookingAt, "Inventory", items, item => [..item is IActionableItem i ? i.Actions : Enumerable.Empty<KeyValuePair<string, IActionableItem.Action>>(), IActionableItem.Drop1, IActionableItem.DropAll, IActionableItem.Delete]);
                AnsiConsole.Cursor.Hide();
                break;
            }
            case ConsoleKeyInfo { Key: ConsoleKey.Enter }:
            {
                if (game.CurrentLevel[game.Player.X, game.Player.Y] is { Items: { Count: > 0 } items })
                    ItemsMenu(game, lookingAt, "Ground", items, _ => [IActionableItem.Pickup]);
                else if (game.CurrentLevel[game.Player.X, game.Player.Y].Template is IActionableTemplate { Actions: { } actions, MenuTitle: var title })
                {
                    var prompt = SelectionPrompt<KeyValuePair<string, IActionableTemplate.Action>>.Default(title, kvp => kvp.Key, IActionableTemplate.Cancel, actions.Append(IActionableTemplate.Cancel));
                    AnsiConsole.Prompt(prompt).Value(game, game.Player.X, game.Player.Y);
                }
                AnsiConsole.Cursor.Hide();
                break;
            }
            case ConsoleKeyInfo { Key: ConsoleKey.Enter or ConsoleKey.Spacebar or ConsoleKey.D5 }:
                game.Activate();
                break;
#if DEBUG
            case ConsoleKeyInfo { Key: ConsoleKey.T, Modifiers: ConsoleModifiers.Control }
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
        static void ItemsMenu(Game game, string lookingAt, string title, IReadOnlyDictionary<Item, int> items, Func<Item, IEnumerable<KeyValuePair<string, IActionableItem.Action>>> extraActions)
        {
            var promptItem = SelectionPrompt<KeyValuePair<Item, int>>.Default(title, kvp => $"{kvp.Key.Name}: {kvp.Value}", new KeyValuePair<Item, int>(default!, 0), items);
            var (item, qty) = AnsiConsole.Prompt(promptItem);
            DrawDungeon(game, lookingAt);
            if (qty == 0)
            {
                AnsiConsole.Cursor.Hide();
                return;
            }

            var promptAction = SelectionPrompt<KeyValuePair<string, IActionableItem.Action>>.Default(item.Name, kvp => kvp.Key, IActionableItem.Cancel, [..extraActions(item), IActionableItem.Cancel]);
            AnsiConsole.Prompt(promptAction).Value(game, item);
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

static string LookingAt(Game game, int x, int y)
{
    var (_, _, tile, i, visible, seen) = game.CurrentLevel[x, y];
    if (visible && i.Count == 1 && i.First() is var item)
        return $"{item.Key.Symbol}: {item.Key.Name} ({item.Value})";
    if (seen)
        return $"{tile.Symbol}: {tile.Name}";
    return "";
}

void Exit(bool wait = false)
{
    MouseSupport(false);
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
    var deco = Decoration.None;

    if ((x, y) == (game.Player.X, game.Player.Y))
    {
        c = game.Player.Symbol;
        fore = game.Player.Color;
    }
    else if (tile.Items.Count >= 1)
    {
        deco = Decoration.Bold | Decoration.Italic;
        if (tile.Items.Count == 1)
        {
            var item = tile.Items.First().Key;
            c = item.Symbol;
            fore = item.Color;
        }
        else
            c = '+';
    }

    if (!tile.IsVisible)
        if (tile.HasBeenSeen)
            fore = fore.Blend(Color.Black, .75f);
        else
            fore = Color.Black;
    if (fore == back)
        c = '█';
    (AnsiConsole.Background, AnsiConsole.Foreground, AnsiConsole.Decoration) = (back, fore, deco);
    AnsiConsole.Write(c);
    AnsiConsole.Decoration = Decoration.None;
}

static void DrawDungeon(Game game, string lookingAt)
{
    Console.SetCursorPosition(AnsiConsole.Profile.Width - game.Width - 1, 0);
    Console.Write('│');
    PrintStat(game.Player.HP, "❤️");
    Console.Write(' ');
    PrintStat(game.Player.Shields, "🛡");
    var header = $"{game.Steps}:{game.Depth}";
    var length = Console.CursorLeft - header.Length + 1;
    if (lookingAt.Length > length)
        lookingAt = $"{lookingAt[..length]}…";
    lookingAt = new string(' ', game.Width) + lookingAt;
    if (lookingAt.Length > length)
        lookingAt = $"{lookingAt[(lookingAt.Length - length)..]}";
    header = lookingAt + " " + header;
    Console.SetCursorPosition(AnsiConsole.Profile.Width - header.Length, 0);
    Console.Write(header);
    Console.SetCursorPosition(AnsiConsole.Profile.Width - game.Width - 1, 1);
    Console.Write('├');
    for (int x = 0; x < game.Width; x++)
        Console.Write('─');
    Console.WriteLine();
    for (int y = 0; y < game.Height; y++)
    {
        AnsiConsole.Cursor.MoveRight(AnsiConsole.Profile.Width - game.Width - 1);
        Console.Write('│');
        for (int x = 0; x < game.Width; x++)
            WriteTile(game, x, y);
        Console.WriteLine();
        AnsiConsole.Reset();
    }
    AnsiConsole.Reset();
    AnsiConsole.Cursor.MoveRight(AnsiConsole.Profile.Width - game.Width - 1);
    Console.Write('└');
    for (int x = 0; x < game.Width; x++)
        Console.Write('─');

    Console.SetCursorPosition(0, 1);

    static void PrintStat(int stat, string text)
    {
        if (stat >= 10)
            Console.Write($"{text} :{stat}");
        else
            for (int i = 0; i < stat; i++)
                Console.Write(text);
    }
}

static void MouseSupport(bool enable)
{
    if (enable)
        AnsiConsole.Console.WriteAnsi(w => w.EnableMouse());
    else
        AnsiConsole.Console.WriteAnsi(w => w.DisableMouse());
}
