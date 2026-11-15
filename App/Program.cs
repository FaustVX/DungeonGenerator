using System.Collections.Frozen;
using System.Diagnostics;
using DungeonGenerator.App;
using DungeonGenerator.App.Creature;
using DungeonGenerator.App.Helpers;
using DungeonGenerator.App.Item;
using DungeonGenerator.App.Tile;
using Spectre.Console;

var alternateScreen = false;
Console.CursorVisible = false;
AnsiConsole.Console.WriteAnsi(w => w.ExitAltScreen()); // To clear the alternate screen if previously wrongly exited
AnsiConsole.Console.WriteAnsi(w => w.EnterAltScreen().CursorHome()); alternateScreen = true;
Console.CancelKeyPress += (s, e) => Exit();
try
{
    var mousePos = (x: 0, y: 0);
    ConsoleRead.MouseEvent lastMouseEvent = default;
    var seed = args is [var arg0]
            ? int.TryParse(arg0, out var s) ? s : arg0.GetHashCode()
            : Random.Shared.Next();
    var game = new Game(100, 20, seed);
    var gameView = new ViewPort(game.Width, game.Height, game.Width, game.Height);

    Run: while (game.Player.HP > 0)
    {
        AnsiConsole.MouseSupport(true);
        var lookingAt = LookingAt(game, mousePos.x, mousePos.y);
        (gameView.X, gameView.Y) = (mousePos.x - gameView.Width / 2, mousePos.y - gameView.Height / 2);
        DrawDungeon(game, lookingAt, lastMouseEvent, gameView);
        switch (AnsiConsole.Console.Input.ReadKeyOrMouse())
        {
            case ConsoleKeyInfo { Key: ConsoleKey.Escape }:
                var exit = SelectionPrompt<bool>.Default("Confirm quit", b => b ? "Yes, quit" : "No, stay", false, [false, true])
                    .Prompt();
                if (exit)
                    break Run;
                AnsiConsole.Cursor.Hide();
                break;
            case ConsoleRead.MouseEvent mouse:
            {
                lastMouseEvent = mouse;
                if (mouse is not { PressedOrMovement: true, Button: ConsoleRead.MouseButton.Movement })
                    break;
                if (mouse.X - (Console.WindowWidth - gameView.Width) is var x and >= 0 && mouse.Y - 2 is var y and >= 0 && y < gameView.Height )
                    mousePos = (x + gameView.X, y + gameView.Y);
                else
                    mousePos = (game.Player.X, game.Player.Y);
                break;
            }
            case ConsoleKeyInfo { Key: ConsoleKey.D1 }:
                _ = (game.MoveDown() && game.MoveLeft()) || (game.MoveLeft() && game.MoveDown());
                mousePos = (game.Player.X, game.Player.Y);
                break;
            case ConsoleKeyInfo { Key: ConsoleKey.DownArrow or ConsoleKey.D2 }:
                game.MoveDown();
                mousePos = (game.Player.X, game.Player.Y);
                break;
            case ConsoleKeyInfo { Key: ConsoleKey.D3 }:
                _ = (game.MoveDown() && game.MoveRight()) || (game.MoveRight() && game.MoveDown());
                mousePos = (game.Player.X, game.Player.Y);
                break;
            case ConsoleKeyInfo { Key: ConsoleKey.LeftArrow or ConsoleKey.D4 }:
                game.MoveLeft();
                mousePos = (game.Player.X, game.Player.Y);
                break;
            case ConsoleKeyInfo { Key: ConsoleKey.RightArrow or ConsoleKey.D6 }:
                game.MoveRight();
                mousePos = (game.Player.X, game.Player.Y);
                break;
            case ConsoleKeyInfo { Key: ConsoleKey.D7 }:
                _ = (game.MoveUp() && game.MoveLeft()) || (game.MoveLeft() && game.MoveUp());
                mousePos = (game.Player.X, game.Player.Y);
                break;
            case ConsoleKeyInfo { Key: ConsoleKey.UpArrow or ConsoleKey.D8 }:
                game.MoveUp();
                mousePos = (game.Player.X, game.Player.Y);
                break;
            case ConsoleKeyInfo { Key: ConsoleKey.D9 }:
                _ = (game.MoveUp() && game.MoveRight()) || (game.MoveRight() && game.MoveUp());
                mousePos = (game.Player.X, game.Player.Y);
                break;
            case ConsoleKeyInfo { Key: ConsoleKey.E }:
            {
                switch (SelectionPrompt<int>.Default("Player", PlayerStorage, -1, [..game.Player.Items.Count > 0 ? [1] : Enumerable.Empty<int>(), ..game.Player.Equipments.Count > 0 ? [2] : Enumerable.Empty<int>(), 3, -1]).Prompt())
                {
                    case 1:
                        DrawDungeon(game, lookingAt, lastMouseEvent, gameView);
                        ItemsMenu(game, lookingAt, "Inventory", game.Player.Items, item => item.Actions(ItemLocation.Inventory), gameView);
                        break;
                    case 2:
                        DrawDungeon(game, lookingAt, lastMouseEvent, gameView);
                        var equipment = SelectionPrompt<KeyValuePair<EquipmentType, Item>>.Default("Which equipment", kvp => $"{kvp.Key}: {kvp.Value.Name}{(kvp.Value is IDurabilityItem { Durability: var d } ? $" ({d})" : "")}", default, game.Player.Equipments)
                            .Prompt();
                        if (equipment is (not EquipmentType.None, IEquipItem equip))
                        {
                            DrawDungeon(game, lookingAt, lastMouseEvent, gameView);
                            var item = (Item)equip;
                            SelectionPrompt<KeyValuePair<string, Item.Action>>.Default(item.Name, kvp => kvp.Key, Item.Cancel, item.Actions(ItemLocation.Equipment))
                                .Prompt().Value(game, FrozenDictionary<Item, int>.Empty, item);
                        }
                        break;
                    case 3:
                        DrawDungeon(game, lookingAt, lastMouseEvent, gameView);
                        _ = SelectionPrompt<string>.Default("Stats", s => s, "", [
                            $"Health: {game.Player.HP}/{game.Player.MaxHP}",
                            $"Force: {game.Player.Force}",
                            $"Shields: {game.Player.Shields}",
                            $"View Distance: {game.Player.ViewDistance}",
                        ]).Prompt();
                        break;
                }
                AnsiConsole.Cursor.Hide();
                break;

                static string PlayerStorage(int i)
                => i switch
                {
                    -1 => "Cancel",
                    +1 => "Inventory",
                    +2 => "Equipments",
                    +3 => "Stats",
                    _ => throw new UnreachableException(),
                };
            }
            case ConsoleKeyInfo { Key: ConsoleKey.Enter }:
            {
                var items = game.CurrentTile.Items;
                var template = game.CurrentTile.Template;
                switch (items, template)
                {
                    case ({ Count: > 0 }, IActionableTemplate { Actions: { } actions, MenuTitle: var title } tile):
                    {
                        var selection = SelectionPrompt<int>.Default("Where", b => b == 1 ? $"Items ({items.Count})" : $"Tile: {tile.Name}", 0, [1, 2])
                            .Prompt();
                        DrawDungeon(game, lookingAt, lastMouseEvent, gameView);
                        if (selection == 1)
                            ItemsMenu(game, lookingAt, "Ground", items, item => item.Actions(ItemLocation.Ground), gameView);
                        else if (selection == 2)
                        {
                            SelectionPrompt<KeyValuePair<string, IActionableTemplate.Action>>.Default(title, kvp => kvp.Key, IActionableTemplate.Cancel, actions.Append(IActionableTemplate.Cancel))
                                .Prompt().Value(game, game.Player.X, game.Player.Y);
                        }
                        break;
                    }
                    case ({ Count: > 0 }, IActivableTemplate tile):
                    {
                        var selection = SelectionPrompt<bool>.Default("Where", b => b ? $"Items ({items.Count})" : $"Use {(tile.Symbol is ']' ? "]]" : tile.Symbol)} {tile.Name}", [true, false])
                            .Prompt();
                        DrawDungeon(game, lookingAt, lastMouseEvent, gameView);
                        if (selection)
                            ItemsMenu(game, lookingAt, "Ground", items, item => item.Actions(ItemLocation.Ground), gameView);
                        else
                            tile.Activate(game, game.Player.X, game.Player.Y);
                        break;
                    }
                    case ({ Count: > 0 }, _):
                        ItemsMenu(game, lookingAt, "Ground", items, item => item.Actions(ItemLocation.Ground), gameView);
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

        static void ItemsMenu(Game game, string lookingAt, string title, IDictionary<Item, int> items, Func<Item, IEnumerable<KeyValuePair<string, Item.Action>>> extraActions, ViewPort gameView)
        {
            var (item, qty) = SelectionPrompt<KeyValuePair<Item, int>>.Default(title, kvp => $"{kvp.Key.Name}: {kvp.Value}", new KeyValuePair<Item, int>(default!, 0), items)
                .Prompt();
            DrawDungeon(game, lookingAt, default, gameView);
            if (qty == 0)
            {
                AnsiConsole.Cursor.Hide();
                return;
            }

            SelectionPrompt<KeyValuePair<string, Item.Action>>.Default(item.Name, kvp => kvp.Key, Item.Cancel, extraActions(item))
                .Prompt().Value(game, items, item);
        }
    }

    if (game.Player.HP <= 0)
    {
        game.Messages.Add($"{DungeonGenerator.App.Messages.Message.TimeStamp(game)} You have [red]died[/], good luck on your next dungeon");
        DrawDungeon(game, "", lastMouseEvent, gameView);
        return;
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
    if (visible && i.Count == 1 && game.CurrentLevel.Creatures.FirstOrDefault(c => (c.X, c.Y) == (x, y)) is {} mob)
        return $"[{mob.Color.ToMarkup()}]{(mob.Symbol is ']' ? "]]" : mob.Symbol)}[/]: {mob.Name}";
    if (visible && i.Count == 1 && i.First() is var item)
        return $"[{item.Key.Color.ToMarkup()}]{(item.Key.Symbol is ']' ? "]]" : item.Key.Symbol)}[/]: {item.Key.Name} ({item.Value})";
    if (seen)
        return $"[{tile.Foreground.ToMarkup()} on {tile.Background.ToMarkup()}]{(tile.Symbol is ']' ? "]]" : tile.Symbol)}[/]: {tile.Name}";
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
    var IsHighlighted = false;

    if ((x, y) == (game.Player.X, game.Player.Y))
    {
        c = game.Player.Symbol;
        fore = game.Player.Color;
    }
    else if (game.CurrentLevel.Creatures.FirstOrDefault(h => h.IsHighlighted && h.Pos == (x, y)) is {} mob)
    {
        IsHighlighted = true;
        c = mob.Symbol;
        fore = mob.Color;
        deco = Decoration.Invert;
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

    if (!IsHighlighted && !tile.IsVisible)
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

static void DrawDungeon(Game game, string lookingAt, ConsoleRead.MouseEvent mouse, ViewPort gameView)
{
    var messageWidth = AnsiConsole.Profile.Width - gameView.Width - 1;
    Console.SetCursorPosition(messageWidth, 0);
    Console.Write('│');
    PrintStat(game.Player.HP, "❤️");
    Console.Write(' ');
    PrintStat(game.Player.Shields, "🛡");
    Console.Write(' ');
    PrintStat(game.Player.Force, "⚔");
    Console.Write(' ');
    var header = $"{game.Steps}:{game.Depth}";
    if (Console.CursorLeft < Console.WindowWidth) // Sometime the cursor is outside the console (I don't know how)
    {
        var length = Console.WindowWidth - Console.CursorLeft - header.Length - 1;
        WithoutMarkup:
        var lengthWitoutMarkup = length + (lookingAt.Length - Markup.Remove(lookingAt).Length);
        lookingAt = lookingAt.Ellipsis(lengthWitoutMarkup);
        lookingAt = new string(' ', gameView.Width) + lookingAt;
        if (lookingAt.Length > lengthWitoutMarkup)
            lookingAt = $"{lookingAt[(lookingAt.Length - lengthWitoutMarkup)..]}";
        header = lookingAt + " " + header;
        try
        {
            Console.Markup(header);
        }
        catch (InvalidOperationException) // Markup exception
        {
            var cleared = Markup.Escape(Markup.Remove(lookingAt));
            if (cleared.Length == lookingAt.Length && cleared == lookingAt)
                throw;
            lookingAt = cleared;
            goto WithoutMarkup;
        }
    }
    Console.SetCursorPosition(messageWidth, 1);
    Console.Write('├');
    for (int x = 0; x < gameView.Width; x++)
        Console.Write('─');
    Console.WriteLine();
    for (int y = 0; y < gameView.Height; y++)
    {
        AnsiConsole.Cursor.MoveRight(messageWidth);
        Console.Write('│');
        for (int x = 0; x < gameView.Width; x++)
            WriteTile(game, x + gameView.X, y + gameView.Y);
        Console.WriteLine();
        AnsiConsole.Reset();
    }
    AnsiConsole.Reset();
    AnsiConsole.Cursor.MoveRight(messageWidth);
    Console.Write('┴');
    for (int x = 0; x < gameView.Width; x++)
        Console.Write('─');

    Console.SetCursorPosition(0, gameView.Height + 2);
    for (int x = 0; x < messageWidth; x++)
        Console.Write('─');
    Console.SetCursorPosition(0, gameView.Height + 2 - 1);
    foreach (var message in game.Messages.AsEnumerable().Reverse())
    {
        var text = message.ToString();
        WithoutMarkup:
        int widthWithoutMarkup = messageWidth + (text.Length - Markup.Remove(text).Length);
        var style = "on black";
        var fullMessage = false;
        if (mouse.Y == Console.CursorTop && mouse.X < messageWidth)
        {
            fullMessage = true;
            if (message is DungeonGenerator.App.Messages.IClickableMessage c)
            {
                style = "underline " + style;
                if (mouse is { Button: ConsoleRead.MouseButton.Left, PressedOrMovement: true })
                    c.OnClick(mouse.X);
            }
        }

        try
        {
            if (fullMessage)
                AnsiConsole.MarkupLine($"[{style}]" + text.PadRight(widthWithoutMarkup, ' ') + "[/]");
            else
                AnsiConsole.MarkupLine($"[{style}]" + text.PadRight(widthWithoutMarkup, ' ').Ellipsis(widthWithoutMarkup) + "[/]");
        }
        catch (InvalidOperationException) // Markup exception
        {
            var cleared = Markup.Escape(Markup.Remove(text));
            if (cleared.Length == text.Length && cleared == text)
                throw;
            text = cleared;
            goto WithoutMarkup;
        }
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

class ViewPort(int width, int height, int maxWidth, int maxHeight)
{
    public int X
    {
        get;
        set
        {
            field = value;
            if (field < 0)
                field = 0;
            if (field + Width >= MaxWidth)
                field = MaxWidth - Width;
        }
    }
    public int Y
    {
        get;
        set
        {
            field = value;
            if (field < 0)
                field = 0;
            if (field + Height >= MaxHeight)
                field = MaxHeight - Height;
        }
    }
    public int Width { get; } = width;
    public int Height { get; } = height;
    public int MaxWidth { get; } = maxWidth;
    public int MaxHeight { get; } = maxHeight;
}
