namespace DungeonGenerator.Messages;

public abstract class Message(Game? game)
{
    protected readonly string _timestamp = TimeStamp(game);
    public override abstract string ToString();
    public static implicit operator Message(string message)
    => new StringMessage(message);
    protected string PlayerColor()
    => CreatureColor(game!.Player);
    protected static string CreatureColor(Creature.Creature creature)
    => $"[{creature.Color}]{creature.Name}[/]";
    protected static string ItemColor(Item.Item item)
    => $"[{item.Color}]{item.Name}[/]";
    protected static string TileColor(Tile.TileTemplate tile)
    => $"[{tile.Foreground} on {tile.Background}]{tile.Name}[/]";
    protected static string TimeStamp(Game? game)
    => game is null ? "" : $"[[{game.Steps}:{game.Depth}]]"; // double square bracket for Spectre markup
}

public interface IClickableMessage
{
    public abstract void OnClick(int pos);
}

public class StringMessage(string message) : Message(null)
{
    public override string ToString()
    => message;
}
