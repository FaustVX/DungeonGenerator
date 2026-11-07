namespace DungeonGenerator.Messages;

public abstract class Message()
{
    public override abstract string ToString();
    public static implicit operator Message(string message)
    => new StringMessage(message);
}

public class StringMessage(string message) : Message()
{
    public override string ToString()
    => message;
}
