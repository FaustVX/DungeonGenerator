using System.Diagnostics;
using DungeonGenerator.Tile;

namespace DungeonGenerator.Generator.BSP;

public abstract class Node
{
    public Generator Root
    {
        get
        {
            if (Parent is null)
                return (Generator)this;
            return Parent.Root;
        }
    }
    public int Depth
    {
        get
        {
            if (Parent is null)
                return 0;
            return Parent.Depth + 1;
        }
    }
    public abstract Generator? Parent { get; }
    public abstract int Width { get; }
    public abstract int Height { get; }
    public abstract TileTemplate GetTile(int x, int y);

    public Node? OtherSide
    => Parent?.First != this ? Parent?.First : Parent?.Second;

    protected void CheckWidthHeight((int w, int h) minRoomSize, (int w, int h) maxRoomSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(Width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(Height);
        ArgumentOutOfRangeException.ThrowIfNotInBetween(minRoomSize.w, 0, maxRoomSize.w);
        ArgumentOutOfRangeException.ThrowIfNotInBetween(minRoomSize.h, 0, maxRoomSize.h);
        ArgumentOutOfRangeException.ThrowIfNotInBetween(maxRoomSize.w, minRoomSize.w, Width);
        ArgumentOutOfRangeException.ThrowIfNotInBetween(maxRoomSize.h, minRoomSize.h, Height);
    }

    public Generator MostCommonNode(Node other)
    {
        var set = new HashSet<Generator>(capacity: Depth);
        for (var node = this; node is not null; node = node.Parent)
            if (node is Generator bsp)
                set.Add(bsp);
        for (var node = other; node is not null; node = node.Parent)
            if (set.Contains((Generator)node))
                return (Generator)node;
        throw new UnreachableException();
    }

    public (int x, int y) GetGlobalOrigin()
    {
        if (Parent is null)
            return (0, 0);
        var (x, y) = Parent.GetGlobalOrigin();
        if (Parent.Second == this)
            switch (Parent?.SplitDirection)
            {
                case SplitDirection.Horizontal:
                    x += Parent.SplitDistance;
                    break;
                case SplitDirection.Vertical:
                    y += Parent.SplitDistance;
                    break;
                default:
                    throw new UnreachableException();
            }
        return this is Corridor corridor ? GetPosInCorridor(corridor, x, y) : (x, y);

        static (int x, int y) GetPosInCorridor(Corridor corridor, int x, int y)
        => corridor.Parent.SplitDirection switch
        {
            SplitDirection.Horizontal => (x + corridor.Parent.SplitDistance - corridor.LengthOffset, y + corridor.Position - 1),
            SplitDirection.Vertical => (x + corridor.Position - 1, y + corridor.Parent.SplitDistance - corridor.LengthOffset),
            _ => throw new UnreachableException(),
        };
    }

    public (int x, int y) GetPosRelativeTo(Node other, int x, int y)
    {
        // tasks/20261024-225707-multiple-generators/tasks/20261025-164140-bsp/RelativePos.excalidraw
        if (other == this)
            return (x, y);

        return (x, y) + GetGlobalOrigin() - other.GetGlobalOrigin();
    }

    public Room GetRoomAtPos(int x, int y)
    {
        (x, y) = GetGlobalOrigin() + (x, y);
        var node = Root;
        while (true)
            switch (node.SplitDirection)
            {
                case SplitDirection.Horizontal:
                    switch (x < node.SplitDistance ? node.First : node.Second)
                    {
                        case Room room:
                            return room;
                        case Generator bsp:
                            node = bsp;
                            break;
                    }
                    break;
                case SplitDirection.Vertical:
                    switch (y < node.SplitDistance ? node.First : node.Second)
                    {
                        case Room room:
                            return room;
                        case Generator bsp:
                            node = bsp;
                            break;
                    }
                    break;
                default:
                    throw new UnreachableException();
        }
    }
}
