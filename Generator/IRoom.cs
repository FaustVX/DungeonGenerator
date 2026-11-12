namespace DungeonGenerator.Generator;

public interface IRoom
{
    public abstract int X { get; }
    public abstract int Y { get; }
    public abstract int Width { get; }
    public abstract int Height { get; }
    public bool IsFullRoom => Width > 1 && Height > 1;
    public IReadOnlyList<IRoom> ConnectedTo { get; }
}
