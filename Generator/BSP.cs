using System.Diagnostics;
using DungeonGenerator.Tile;

namespace DungeonGenerator.Generator;

public class BSP : BSPNode, IGenerator
{
    public override int Width { get; }
    public override int Height { get; }
    public List<Room> Rooms { get; } = [];
    public override BSP? Parent { get; }
    public BSPNode First { get; }
    public BSPNode Second { get; }
    public int SplitDistance { get; }
    public SplitDirectionEnum SplitDirection { get; }

    public BSP(int width, int height, (int w, int h) minRoomSize, (int w, int h) maxRoomSize, double fullSizedRoomPercent, RngOrSeed random)
    : this(width, height, minRoomSize, maxRoomSize, fullSizedRoomPercent, random.Random, null, null)
    { }

    private BSP(int width, int height, (int w, int h) minRoomSize, (int w, int h) maxRoomSize, double fullSizedRoomPercent, Random rng, BSP? parent, SplitDirectionEnum? otherDirection)
    {
        Width = width;
        Height = height;
        Parent = parent;
        CheckWidthHeight(minRoomSize, maxRoomSize);
        ArgumentOutOfRangeException.ThrowIfNegative(fullSizedRoomPercent);
        switch (GetSplitDirection(rng, width, height, minRoomSize, maxRoomSize, otherDirection))
        {
            case (SplitDirectionEnum.Horizontal, var otherDir, var size):
                SplitDirection = SplitDirectionEnum.Horizontal;
                SplitDistance = size;
                First = size <= maxRoomSize.w ? new Room(size, height, minRoomSize, maxRoomSize, fullSizedRoomPercent, rng, this) : new BSP(size, height, minRoomSize, maxRoomSize, fullSizedRoomPercent, rng, this, otherDir);
                Second = width - size <= maxRoomSize.w ? new Room(width - size, height, minRoomSize, maxRoomSize, fullSizedRoomPercent, rng, this) : new BSP(width - size, height, minRoomSize, maxRoomSize, fullSizedRoomPercent, rng, this, otherDir);
                break;
            case (SplitDirectionEnum.Vertical, var otherDir, var size):
                SplitDirection = SplitDirectionEnum.Vertical;
                SplitDistance = size;
                First = size <= maxRoomSize.h ? new Room(width, size, minRoomSize, maxRoomSize, fullSizedRoomPercent, rng, this) : new BSP(width, size, minRoomSize, maxRoomSize, fullSizedRoomPercent, rng, this, otherDir);
                Second = height - size <= maxRoomSize.h ? new Room(width, height - size, minRoomSize, maxRoomSize, fullSizedRoomPercent, rng, this) : new BSP(width, height - size, minRoomSize, maxRoomSize, fullSizedRoomPercent, rng, this, otherDir);
                break;
            default:
                throw new UnreachableException();
        };
    }

    static (SplitDirectionEnum dir, SplitDirectionEnum? other, int size) GetSplitDirection(Random rng, int width, int height, (int w, int h) minRoomSize, (int w, int h) maxRoomSize, SplitDirectionEnum? otherDirection)
    {
        var w = CanSplit(minRoomSize.w, width, maxRoomSize.w, rng);
        var h = CanSplit(minRoomSize.h, height, maxRoomSize.h, rng);
        if (!w && !h)
            throw new UnreachableException();

        var dir = (stackalloc SplitDirectionEnum[1]);
        rng.GetItems([
            // ..otherDirection is {} other ? [other] : ReadOnlySpan<SplitDirection>.Empty,
            ..w ? [SplitDirectionEnum.Horizontal] : ReadOnlySpan<SplitDirectionEnum>.Empty,
            ..h ? [SplitDirectionEnum.Vertical] : ReadOnlySpan<SplitDirectionEnum>.Empty
        ], dir);

        return dir[0] switch
        {
            SplitDirectionEnum.Horizontal => (SplitDirectionEnum.Horizontal, h ? SplitDirectionEnum.Vertical : null, rng.Next(minRoomSize.w, width - minRoomSize.w)),
            SplitDirectionEnum.Vertical => (SplitDirectionEnum.Vertical, w ? SplitDirectionEnum.Horizontal : null, rng.Next(minRoomSize.h, height - minRoomSize.h)),
            _ => throw new UnreachableException(),
        };

        static bool CanSplit(int min, int size, int max, Random rng)
        {
            if (size <= min)
                return false;
            if (size > max)
                return true;
            return rng.NextBool();
        }
    }

    public override TileTemplate GetTile(int x, int y)
    {
        ArgumentOutOfRangeException.ThrowIfNotInBetween(x, 0, Width - 1);
        ArgumentOutOfRangeException.ThrowIfNotInBetween(y, 0, Height - 1);
        switch (SplitDirection)
        {
            case SplitDirectionEnum.Horizontal:
                if (x < SplitDistance)
                    return First.GetTile(x, y);
                return Second.GetTile(x - SplitDistance, y);
            case SplitDirectionEnum.Vertical:
                if (y < SplitDistance)
                    return First.GetTile(x, y);
                return Second.GetTile(x, y - SplitDistance);
            default:
                throw new UnreachableException();
        }
    }

    public enum SplitDirectionEnum
    {
        Horizontal,
        Vertical,
    }

    public sealed class Room : BSPNode
    {
        public Room(int width, int height, (int w, int h) minRoomSize, (int w, int h) maxRoomSize, double fullSizedRoomPercent, Random rng, BSP parent)
        {
            Width = width;
            Height = height;
            Parent = parent;
            CheckWidthHeight(minRoomSize, maxRoomSize);
            ArgumentOutOfRangeException.ThrowIfNegative(fullSizedRoomPercent);
            Root.Rooms.Add(this);
        }

        public override BSP Parent { get; }
        public override int Width { get; }
        public override int Height { get; }
        public int TopWallWidth { get; } = 1;
        public int BottomWallWidth { get; } = 1;
        public int LeftWallWidth { get; } = 1;
        public int RightWallWidth { get; } = 1;

        public override TileTemplate GetTile(int x, int y)
        {
            ArgumentOutOfRangeException.ThrowIfNotInBetween(x, 0, Width - 1);
            ArgumentOutOfRangeException.ThrowIfNotInBetween(y, 0, Height - 1);
            if (x < LeftWallWidth || y < TopWallWidth || x >= Width - LeftWallWidth || y >= Height - BottomWallWidth)
                return TileTemplate.Wall;
            return TileTemplate.Ground;
        }

        public Room? LeftRoom(int y)
        {
            foreach (var room in Root.Rooms)
            {
                (var x, y) = GetPosRelativeTo(room, 0, y);
                if (x == room.Width && y >= 0 && y < room.Height)
                    return room;
            }
            return null;
        }

        public Room? RightRoom(int y)
        {
            foreach (var room in Root.Rooms)
            {
                (var x, y) = GetPosRelativeTo(room, Width, y);
                if (x == 0 && y >= 0 && y < room.Height)
                    return room;
            }
            return null;
        }

        public Room? UpRoom(int x)
        {
            foreach (var room in Root.Rooms)
            {
                (x, var y) = GetPosRelativeTo(room, x, 0);
                if (y == room.Height && x >= 0 && x < room.Width)
                    return room;
            }
            return null;
        }

        public Room? DownRoom(int x)
        {
            foreach (var room in Root.Rooms)
            {
                (x, var y) = GetPosRelativeTo(room, x, Height);
                if (y == 0 && x >= 0 && x < room.Width)
                    return room;
            }
            return null;
        }
    }
}

public abstract class BSPNode
{
    public BSP Root
    {
        get
        {
            if (Parent is null)
                return (BSP)this;
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
    public abstract BSP? Parent { get; }
    public abstract int Width { get; }
    public abstract int Height { get; }
    public abstract TileTemplate GetTile(int x, int y);

    protected void CheckWidthHeight((int w, int h) minRoomSize, (int w, int h) maxRoomSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(Width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(Height);
        ArgumentOutOfRangeException.ThrowIfNotInBetween(minRoomSize.w, 0, maxRoomSize.w);
        ArgumentOutOfRangeException.ThrowIfNotInBetween(minRoomSize.h, 0, maxRoomSize.h);
        ArgumentOutOfRangeException.ThrowIfNotInBetween(maxRoomSize.w, minRoomSize.w, Width);
        ArgumentOutOfRangeException.ThrowIfNotInBetween(maxRoomSize.h, minRoomSize.h, Height);
    }

    public BSP MostCommonNode(BSPNode other)
    {
        var set = new HashSet<BSP>(capacity: Depth);
        for (var node = this; node is not null; node = node.Parent)
            if (node is BSP bsp)
                set.Add(bsp);
        for (var node = other; node is not null; node = node.Parent)
            if (set.Contains((BSP)node))
                return (BSP)node;
        throw new UnreachableException();
    }

    public (int x, int y) GetGlobalOrigin()
    {
        if (Parent is not null)
        {
            var (x, y) = Parent.GetGlobalOrigin();
            if (Parent.Second == this)
                switch (Parent?.SplitDirection)
                {
                    case BSP.SplitDirectionEnum.Horizontal:
                        x += Parent.SplitDistance;
                        break;
                    case BSP.SplitDirectionEnum.Vertical:
                        y += Parent.SplitDistance;
                        break;
                }
            return (x, y);
        }
        return (0, 0);
    }

    public (int x, int y) GetPosRelativeTo(BSPNode other, int x, int y)
    {
        // tasks/20261024-225707-multiple-generators/tasks/20261025-164140-bsp/RelativePos.excalidraw
        if (other == this)
            return (x, y);

        return (x, y) + GetGlobalOrigin() - other.GetGlobalOrigin();
    }

    public BSP.Room GetRoomAtPos(int x, int y)
    {
        (x, y) = GetGlobalOrigin() + (x, y);
        var node = Root;
        while (true)
            switch (node.SplitDirection)
            {
                case BSP.SplitDirectionEnum.Horizontal:
                    switch (x < node.SplitDistance ? node.First : node.Second)
                    {
                        case BSP.Room room:
                            return room;
                        case BSP bsp:
                            node = bsp;
                            break;
                    }
                    break;
                case BSP.SplitDirectionEnum.Vertical:
                    switch (y < node.SplitDistance ? node.First : node.Second)
                    {
                        case BSP.Room room:
                            return room;
                        case BSP bsp:
                            node = bsp;
                            break;
                    }
                    break;
                default:
                    throw new UnreachableException();
        }
    }
}
