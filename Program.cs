using System.Diagnostics;
using DungeonGenerator;
using DungeonGenerator.Helpers;
using DungeonGenerator.Item;
using DungeonGenerator.Tile;
using Spectre.Console;

AnsiConsole.MouseSupport(true);

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
                var exit = SelectionPrompt<bool>.Default("Confirm quit", b => b ? "Yes, quit" : "No, stay", false, [false, true])
                    .Prompt();
                if (exit)
                    break Run;
                AnsiConsole.Cursor.Hide();
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
                ItemsMenu(game, lookingAt, "Inventory", items, item => [..item.Actions, Item.Drop1, Item.DropAll, Item.Delete]);
                AnsiConsole.Cursor.Hide();
                break;
            }
            case ConsoleKeyInfo { Key: ConsoleKey.Enter }:
            {
                var items = game.CurrentTile.Items;
                var template = game.CurrentTile.Template;
                switch (items, template)
                {
                    case ({ Count: > 0 }, IActionableTemplate { Actions: { } actions, MenuTitle: var title } tile):
                    {
                        var selection = SelectionPrompt<bool>.Default("Where", b => b ? $"Items ({items.Count})" : $"Tile: {tile.Name}", [true, false])
                            .Prompt();
                        DrawDungeon(game, lookingAt);
                        if (selection)
                            ItemsMenu(game, lookingAt, "Ground", items, _ => [Item.Pickup]);
                        else
                        {
                            SelectionPrompt<KeyValuePair<string, IActionableTemplate.Action>>.Default(title, kvp => kvp.Key, IActionableTemplate.Cancel, actions.Append(IActionableTemplate.Cancel))
                                .Prompt().Value(game, game.Player.X, game.Player.Y);
                        }
                        break;
                    }
                    case ({ Count: > 0 }, IActivableTemplate tile):
                    {
                        var selection = SelectionPrompt<bool>.Default("Where", b => b ? $"Items ({items.Count})" : $"Use {tile.Symbol} {tile.Name}", [true, false])
                            .Prompt();
                        DrawDungeon(game, lookingAt);
                        if (selection)
                            ItemsMenu(game, lookingAt, "Ground", items, _ => [Item.Pickup]);
                        else
                            tile.Activate(game, game.Player.X, game.Player.Y);
                        break;
                    }
                    case ({ Count: > 0 }, _):
                        ItemsMenu(game, lookingAt, "Ground", items, _ => [Item.Pickup]);
                        break;
                    case (_, IActionableTemplate { Actions: { } actions, MenuTitle: var title }):
                    {
                        SelectionPrompt<KeyValuePair<string, IActionableTemplate.Action>>.Default(title, kvp => kvp.Key, IActionableTemplate.Cancel, actions.Append(IActionableTemplate.Cancel))
                            .Prompt().Value(game, game.Player.X, game.Player.Y);
                        break;
                    }
                }
                AnsiConsole.Cursor.Hide();
                break;
            }
            case ConsoleKeyInfo { Key: ConsoleKey.Enter or ConsoleKey.Spacebar or ConsoleKey.D5 }:
                game.Activate();
                break;
#if DEBUG
            case ConsoleKeyInfo { Key: ConsoleKey.T, Modifiers: ConsoleModifiers.Control }
            when game.CurrentTile.Template is IStairTemplate { Direction: var dir }:
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
        static void ItemsMenu(Game game, string lookingAt, string title, IReadOnlyDictionary<Item, int> items, Func<Item, IEnumerable<KeyValuePair<string, Item.Action>>> extraActions)
        {
            var (item, qty) = SelectionPrompt<KeyValuePair<Item, int>>.Default(title, kvp => $"{kvp.Key.Name}: {kvp.Value}", new KeyValuePair<Item, int>(default!, 0), items)
                .Prompt();
            DrawDungeon(game, lookingAt);
            if (qty == 0)
            {
                AnsiConsole.Cursor.Hide();
                return;
            }

            SelectionPrompt<KeyValuePair<string, Item.Action>>.Default(item.Name, kvp => kvp.Key, Item.Cancel, [..extraActions(item), Item.Cancel])
                .Prompt().Value(game, item);
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
    AnsiConsole.MouseSupport(false);
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
    else if (tile.IsVisible && game.CurrentLevel.Creatures.FirstOrDefault(c => (c.X, c.Y) == (x, y)) is {} creature)
    {
        c = creature.Symbol;
        fore = creature.Color;
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
    var messageWidth = AnsiConsole.Profile.Width - game.Width - 1;
    Console.SetCursorPosition(messageWidth, 0);
    Console.Write('│');
    PrintStat(game.Player.HP, "❤️");
    Console.Write(' ');
    PrintStat(game.Player.Shields, "🛡");
    var header = $"{game.Steps}:{game.Depth}";
    var length = Console.CursorLeft - header.Length + 1;
    lookingAt = lookingAt.Ellipsis(length);
    lookingAt = new string(' ', game.Width) + lookingAt;
    if (lookingAt.Length > length)
        lookingAt = $"{lookingAt[(lookingAt.Length - length)..]}";
    header = lookingAt + " " + header;
    Console.SetCursorPosition(AnsiConsole.Profile.Width - header.Length, 0);
    Console.Write(header);
    Console.SetCursorPosition(messageWidth, 1);
    Console.Write('├');
    for (int x = 0; x < game.Width; x++)
        Console.Write('─');
    Console.WriteLine();
    for (int y = 0; y < game.Height; y++)
    {
        AnsiConsole.Cursor.MoveRight(messageWidth);
        Console.Write('│');
        for (int x = 0; x < game.Width; x++)
            WriteTile(game, x, y);
        Console.WriteLine();
        AnsiConsole.Reset();
    }
    AnsiConsole.Reset();
    AnsiConsole.Cursor.MoveRight(messageWidth);
    Console.Write('┴');
    for (int x = 0; x < game.Width; x++)
        Console.Write('─');

    Console.SetCursorPosition(0, game.Height + 2);
    for (int x = 0; x < messageWidth; x++)
        Console.Write('─');
    Console.SetCursorPosition(0, game.Height + 2 - 1);
    foreach (var message in game.Messages.AsEnumerable().Reverse())
    {
        var text = message.ToString();
        int widthWithoutMarkup = messageWidth + (text.Length - Markup.Remove(text).Length);
        AnsiConsole.MarkupLine($"[on black]" + text.PadRight(widthWithoutMarkup, ' ').Ellipsis(widthWithoutMarkup) + "[/]");
        AnsiConsole.Cursor.MoveUp(2);
    }
    AnsiConsole.Write($" {game.Messages.Count} message(s) ".PadCenter(messageWidth, '─'));
    Console.Write(game.Messages.IsFull ? '┼' : '┤');

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
