using System.Diagnostics;
using DungeonGenerator.Tile;

namespace DungeonGenerator.Generator;

public class BSP : IGenerator, BSP.INode
{
    public int Width { get; }
    public int Height { get; }
    private readonly int _splitDistance;
    private readonly SplitDirection _splitDirection;
    private readonly List<Room> _rooms = [];
    public BSP? Parent { get; }
    public INode First { get; }
    public INode Second { get; }

    public BSP(int width, int height, (int w, int h) minRoomSize, (int w, int h) maxRoomSize, double fullSizedRoomPercent, RngOrSeed random)
    : this(width, height, minRoomSize, maxRoomSize, fullSizedRoomPercent, random.Random, null, null)
    { }

    private BSP(int width, int height, (int w, int h) minRoomSize, (int w, int h) maxRoomSize, double fullSizedRoomPercent, Random rng, BSP? parent, SplitDirection? otherDirection)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        ArgumentOutOfRangeException.ThrowIfNotInBetween(minRoomSize.w, 0, maxRoomSize.w);
        ArgumentOutOfRangeException.ThrowIfNotInBetween(minRoomSize.h, 0, maxRoomSize.h);
        ArgumentOutOfRangeException.ThrowIfNotInBetween(maxRoomSize.w, minRoomSize.w, width);
        ArgumentOutOfRangeException.ThrowIfNotInBetween(maxRoomSize.h, minRoomSize.h, height);
        ArgumentOutOfRangeException.ThrowIfNegative(fullSizedRoomPercent);
        Width = width;
        Height = height;
        Parent = parent;
        switch (GetSplitDirection(rng, width, height, minRoomSize, maxRoomSize, otherDirection))
        {
            case (SplitDirection.Horizontal, var otherDir, var size):
                _splitDirection = SplitDirection.Horizontal;
                _splitDistance = size;
                First = size <= maxRoomSize.w ? new Room(this, size, height) : new BSP(size, height, minRoomSize, maxRoomSize, fullSizedRoomPercent, rng, this, otherDir);
                Second = width - size <= maxRoomSize.w ? new Room(this, width - size, height) : new BSP(width - size, height, minRoomSize, maxRoomSize, fullSizedRoomPercent, rng, this, otherDir);
                break;
            case (SplitDirection.Vertical, var otherDir, var size):
                _splitDirection = SplitDirection.Vertical;
                _splitDistance = size;
                First = size <= maxRoomSize.h ? new Room(this, width, size) : new BSP(width, size, minRoomSize, maxRoomSize, fullSizedRoomPercent, rng, this, otherDir);
                Second = height - size <= maxRoomSize.h ? new Room(this, width, height - size) : new BSP(width, height - size, minRoomSize, maxRoomSize, fullSizedRoomPercent, rng, this, otherDir);
                break;
            default:
                throw new UnreachableException();
        };
    }

    static (SplitDirection dir, SplitDirection? other, int size) GetSplitDirection(Random rng, int width, int height, (int w, int h) minRoomSize, (int w, int h) maxRoomSize, SplitDirection? otherDirection)
    {
        var w = CanSplit(minRoomSize.w, width, maxRoomSize.w, rng);
        var h = CanSplit(minRoomSize.h, height, maxRoomSize.h, rng);
        if (!w && !h)
            throw new UnreachableException();

        var dir = (stackalloc SplitDirection[1]);
        rng.GetItems([
            // ..otherDirection is {} other ? [other] : ReadOnlySpan<SplitDirection>.Empty,
            ..w ? [SplitDirection.Horizontal] : ReadOnlySpan<SplitDirection>.Empty,
            ..h ? [SplitDirection.Vertical] : ReadOnlySpan<SplitDirection>.Empty
        ], dir);

        return dir[0] switch
        {
            SplitDirection.Horizontal => (SplitDirection.Horizontal, h ? SplitDirection.Vertical : null, rng.Next(minRoomSize.w, width - minRoomSize.w)),
            SplitDirection.Vertical => (SplitDirection.Vertical, w ? SplitDirection.Horizontal : null, rng.Next(minRoomSize.h, height - minRoomSize.h)),
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

    public TileTemplate GetTile(int x, int y)
    {
        switch (_splitDirection)
        {
            case SplitDirection.Horizontal:
                if (x < _splitDistance)
                    return First.GetTile(x, y);
                return Second.GetTile(x - _splitDistance, y);
            case SplitDirection.Vertical:
                if (y < _splitDistance)
                    return First.GetTile(x, y);
                return Second.GetTile(x, y - _splitDistance);
            default:
                throw new UnreachableException();
        }
    }

    public interface INode
    {
        public BSP Root
        {
            get
            {
                if (Parent is null)
                    return (BSP)this;
                return ((INode)Parent).Root;
            }
        }
        public int Depth
        {
            get
            {
                if (Parent is null)
                    return 0;
                return ((INode)Parent).Depth + 1;
            }
        }
        public BSP? Parent { get; }
        public int Width { get; }
        public int Height { get; }
        public TileTemplate GetTile(int x, int y);

        public BSP MostCommonNode(INode other)
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
                var (x, y) = ((INode)Parent!).GetGlobalOrigin();
                if (Parent.Second == this)
                    switch (Parent?._splitDirection)
                    {
                        case SplitDirection.Horizontal:
                            x += Parent._splitDistance;
                            break;
                        case SplitDirection.Vertical:
                            y += Parent._splitDistance;
                            break;
                    }
                return (x, y);
            }
            return (0, 0);
        }

        public (int x, int y) GetPosRelativeTo(INode other, int x, int y)
        {
            // tasks/20261024-225707-multiple-generators/tasks/20261025-164140-bsp/RelativePos.excalidraw
            if (other == this)
                return (x, y);

            return (x, y) + GetGlobalOrigin() - other.GetGlobalOrigin();
        }
    }

    private enum SplitDirection
    {
        Horizontal,
        Vertical,
    }

    private sealed record class Room : INode
    {
        public Room(BSP parent, int width, int height)
        {
            Parent = parent;
            Width = width;
            Height = height;
            ((INode)this).Root._rooms.Add(this);
        }

        public BSP Parent { get; }
        public int Width { get; }
        public int Height { get; }

        public TileTemplate GetTile(int x, int y)
        {
            if (x == 0 || y == 0 || x == Width - 1 || y == Height - 1)
                return TileTemplate.Wall;
            return TileTemplate.Ground;
        }

        public Room? LeftRoom(int pos)
        {
            foreach (var room in ((INode)this).Root._rooms)
            {
                var (x, y) = ((INode)this).GetPosRelativeTo(room, 0, pos);
                if (x == room.Width && y >= 0 && y < room.Height)
                    return room;
            }
            return null;
        }

        public Room? RightRoom(int pos)
        {
            foreach (var room in ((INode)this).Root._rooms)
            {
                var (x, y) = ((INode)this).GetPosRelativeTo(room, Width, pos);
                if (x == 0 && y >= 0 && y < room.Height)
                    return room;
            }
            return null;
        }

        public Room? UpRoom(int pos)
        {
            foreach (var room in ((INode)this).Root._rooms)
            {
                var (x, y) = ((INode)this).GetPosRelativeTo(room, pos, 0);
                if (y == room.Height && x >= 0 && x < room.Width)
                    return room;
            }
            return null;
        }

        public Room? DownRoom(int pos)
        {
            foreach (var room in ((INode)this).Root._rooms)
            {
                var (x, y) = ((INode)this).GetPosRelativeTo(room, pos, Height);
                if (y == 0 && x >= 0 && x < room.Width)
                    return room;
            }
            return null;
        }
    }
}
