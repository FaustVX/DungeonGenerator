namespace DungeonGenerator.Messages;

public abstract class Message()
{
    public override abstract string ToString();
    public static implicit operator Message(string message)
    => new StringMessage(message);
    protected static string CreatureColor(Creature.Creature creature)
    => $"[{creature.Color}]{creature.Name}[/]";
    protected static string ItemColor(Item.Item item)
    => $"[{item.Color}]{item.Name}[/]";
    protected static string TileColor(Tile.TileTemplate tile)
    => $"[{tile.Foreground} on {tile.Background}]{tile.Name}[/]";
}

public class StringMessage(string message) : Message()
{
    public override string ToString()
    => message;
}

public class FightMessage(Creature.Creature attacker, Creature.Creature attacked) : Message
{
    public override string ToString()
    => $"{CreatureColor(attacker)} fights {CreatureColor(attacked)}";
}

public class DropMessage(Creature.Creature creature, int qty, Item.Item item) : Message
{
    public override string ToString()
    => $"{CreatureColor(creature)} dropped {qty} {ItemColor(item)} on the ground";
}

public class PickupMessage(Creature.Creature creature, int qty, Item.Item item) : Message
{
    public override string ToString()
    => $"{CreatureColor(creature)} picked up {qty} {ItemColor(item)} from the ground";
}
