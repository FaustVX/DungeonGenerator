using Spectre.Console;

namespace DungeonGenerator.App.Messages;

public abstract class Message(Game? game, string message)
{
    protected readonly string _message = $"{TimeStamp(game)} {message}";
    public override sealed string ToString()
    => _message;
    public static implicit operator Message(string message)
    => new StringMessage(message);
    protected static string CreatureColor(Creature.Creature creature)
    => $"[{creature.Color.ToMarkup()}]{creature.Name}[/]";
    protected static string ItemColor(Item.Item item)
    => $"[{item.Color.ToMarkup()}]{item.Name}[/]";
    protected static string TileColor(Tile.TileTemplate tile)
    => $"[{tile.Foreground.ToMarkup()} on {tile.Background.ToMarkup()}]{tile.Name}[/]";
    protected static string WithColor<T>(T obj, Color color)
    => $"[{color.ToMarkup()}]{obj}[/]";
    public static string TimeStamp(Game? game)
    => game is null ? "" : $"[[{game.Steps}:{game.Depth}]]"; // double square bracket for Spectre markup
}

public interface IClickableMessage
{
    public abstract void OnClick(int pos);
}

public class StringMessage(string message) : Message(null, message);
