namespace DungeonGenerator.Messages;

public abstract class Message(Game? game)
{
    protected readonly string _timestamp = TimeStamp(game);
    public override abstract string ToString();
    public static implicit operator Message(string message)
    => new StringMessage(message);
    protected static string CreatureColor(Creature.Creature creature)
    => $"[{creature.Color}]{creature.Name}[/]";
    protected static string ItemColor(Item.Item item)
    => $"[{item.Color}]{item.Name}[/]";
    protected static string TileColor(Tile.TileTemplate tile)
    => $"[{tile.Foreground} on {tile.Background}]{tile.Name}[/]";
    protected static string TimeStamp(Game? game)
    => game is null ? "" : $"[[{game.Steps}:{game.Depth}]]"; // double square bracket for Spectre markup
}

public class StringMessage(string message) : Message(null)
{
    public override string ToString()
    => message;
}

public class FightMessage(Game game, Creature.Creature attacker, Creature.Creature attacked) : Message(game)
{
    public override string ToString()
    => $"{_timestamp} {CreatureColor(attacker)} fights {CreatureColor(attacked)}";
}

public class DropMessage(Game game, Creature.Creature creature, int qty, Item.Item item) : Message(game)
{
    public override string ToString()
    => $"{_timestamp} {CreatureColor(creature)} dropped {qty} {ItemColor(item)} on the ground";
}

public class PickupMessage(Game game, Creature.Creature creature, int qty, Item.Item item) : Message(game)
{
    public override string ToString()
    => $"{_timestamp} {CreatureColor(creature)} picked up {qty} {ItemColor(item)} from the ground";
}

#pragma warning disable CS9107 // Parameter is captured into the state of the enclosing type and its value is also passed to the base constructor. The value might be captured by the base class as well.
public class SawMessage(Game game, Creature.Mob mob) : Message(game)
#pragma warning restore CS9107 // Parameter is captured into the state of the enclosing type and its value is also passed to the base constructor. The value might be captured by the base class as well.
{
    public override string ToString()
    => $"{_timestamp} {CreatureColor(game.Player)} saw {CreatureColor(mob)}";
}
