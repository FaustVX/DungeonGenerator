using System.Diagnostics;
using DungeonGenerator.Helpers;
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
        if (this is Corridor corridor)
            return GetPosInCorridor(corridor, x, y);
        if (Parent.First != null && Parent.First != this)
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
        return (x, y);

        static (int x, int y) GetPosInCorridor(Corridor corridor, int x, int y)
        => corridor.Parent.SplitDirection switch
        {
            SplitDirection.Horizontal => (x + corridor.Parent.SplitDistance - corridor.LengthOffset, y + corridor.Position),
            SplitDirection.Vertical => (x + corridor.Position, y + corridor.Parent.SplitDistance - corridor.LengthOffset),
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

    public CorridorOrRoom GetNodeAt(int x, int y)
    {
        Node node = this;
        while (node is Generator bsp)
        {
            if (bsp.Corridor is {} corridor)
            {
                var (corridorX, corridorY) = bsp.GetPosRelativeTo(corridor, x, y);
                if (0 <= corridorX && corridorX < corridor.Width && 0 <= corridorY && corridorY < corridor.Height)
                    return corridor;
            }

            switch (bsp.SplitDirection)
            {
                case SplitDirection.Horizontal:
                    if (x < bsp.SplitDistance)
                        node = bsp.First;
                    else
                    {
                        x -= bsp.SplitDistance;
                        node = bsp.Second;
                    }
                    break;
                case SplitDirection.Vertical:
                    if (y < bsp.SplitDistance)
                        node = bsp.First;
                    else
                    {
                        y -= bsp.SplitDistance;
                        node = bsp.Second;
                    }
                    break;
                default:
                    throw new UnreachableException();
            }
        }

        return node switch
        {
            Corridor c => c,
            Room r => r,
            _ => throw new UnreachableException(),
        };
    }

    public int TopMostWalkable()
    => this switch
    {
        Room r => r.TopWallWidth,
        Generator g => Math.Min(g.First.GetPosRelativeTo(this, 0, g.First.TopMostWalkable()).y,
                            g.Second.GetPosRelativeTo(this, 0, g.Second.TopMostWalkable()).y),
        _ => throw new UnreachableException(),
    };

    public int BottomMostWalkable()
    => this switch
    {
        Room r => r.Height - r.BottomWallWidth - 1,
        Generator g => Math.Max(g.First.GetPosRelativeTo(this, 0, g.First.BottomMostWalkable()).y,
                            g.Second.GetPosRelativeTo(this, 0, g.Second.BottomMostWalkable()).y),
        _ => throw new UnreachableException(),
    };

    public int LeftMostWalkable()
    => this switch
    {
        Room r => r.LeftWallWidth,
        Generator g => Math.Min(g.First.GetPosRelativeTo(this, g.First.LeftMostWalkable(), 0).x,
                            g.Second.GetPosRelativeTo(this, g.Second.LeftMostWalkable(), 0).x),
        _ => throw new UnreachableException(),
    };

    public int RightMostWalkable()
    => this switch
    {
        Room r => r.Width - r.RightWallWidth - 1,
        Generator g => Math.Max(g.First.GetPosRelativeTo(this, g.First.RightMostWalkable(), 0).x,
                            g.Second.GetPosRelativeTo(this, g.Second.RightMostWalkable(), 0).x),
        _ => throw new UnreachableException(),
    };
}

public readonly union CorridorOrRoom(Corridor, Room) : IRoom
{
    public int X
    => this switch
    {
        Corridor c => c.X,
        Room r => r.X,
    };

    public int Y
    => this switch
    {
        Corridor c => c.Y,
        Room r => r.Y,
    };
    public int Width
    => this switch
    {
        Corridor c => c.Width,
        Room r => r.Width,
    };

    public int Height
    => this switch
    {
        Corridor c => c.Height,
        Room r => r.Height,
    };
    IReadOnlyList<IRoom> IRoom.ConnectedTo => ConnectedTo;
    public IRoom Room => (IRoom)Value;
    public List<IRoom> ConnectedTo
    => this switch
    {
        Corridor c => c.ConnectedTo,
        Room r => r.ConnectedTo,
    };
}
