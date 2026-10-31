using System.Diagnostics;
using DungeonGenerator.Tile;

namespace DungeonGenerator.Generator;

public class BSP : BSPNode, IGenerator
{
    public override int Width { get; }
    public override int Height { get; }
    public List<BSPRoom> Rooms { get; } = [];
    public override BSP? Parent { get; }
    public Level Level { get; }
    public BSPNode First { get; }
    public BSPNode Second { get; }
    public int SplitDistance { get; }
    public BSPSplitDirection SplitDirection { get; }
    public BSPCorridor Corridor { get; }

    public BSP? OtherBSP
    => (BSP?)OtherSide;

    public BSP(int width, int height, (int w, int h) minRoomSize, (int w, int h) maxRoomSize, double fullSizedRoomPercent, RngOrSeed random, Level level)
    : this(width, height, minRoomSize, maxRoomSize, fullSizedRoomPercent, random.Random, null, null, level)
    {
        Rooms[0].GenerateEntrance(random.Random);
        Rooms[^1].GenerateExit(random.Random);
    }

    private BSP(int width, int height, (int w, int h) minRoomSize, (int w, int h) maxRoomSize, double fullSizedRoomPercent, Random rng, BSP? parent, BSPSplitDirection? otherDirection, Level level)
    {
        Width = width;
        Height = height;
        Parent = parent;
        Level = level;
        CheckWidthHeight(minRoomSize, maxRoomSize);
        ArgumentOutOfRangeException.ThrowIfNegative(fullSizedRoomPercent);
        switch (GetSplitDirection(rng, width, height, minRoomSize, maxRoomSize, otherDirection))
        {
            case (BSPSplitDirection.Horizontal, var otherDir, var size):
                SplitDirection = BSPSplitDirection.Horizontal;
                SplitDistance = size;
                First = size <= maxRoomSize.w ? new BSPRoom(size, height, minRoomSize, maxRoomSize, fullSizedRoomPercent, rng, this) : new BSP(size, height, minRoomSize, maxRoomSize, fullSizedRoomPercent, rng, this, otherDir, level);
                Second = width - size <= maxRoomSize.w ? new BSPRoom(width - size, height, minRoomSize, maxRoomSize, fullSizedRoomPercent, rng, this) : new BSP(width - size, height, minRoomSize, maxRoomSize, fullSizedRoomPercent, rng, this, otherDir, level);
                break;
            case (BSPSplitDirection.Vertical, var otherDir, var size):
                SplitDirection = BSPSplitDirection.Vertical;
                SplitDistance = size;
                First = size <= maxRoomSize.h ? new BSPRoom(width, size, minRoomSize, maxRoomSize, fullSizedRoomPercent, rng, this) : new BSP(width, size, minRoomSize, maxRoomSize, fullSizedRoomPercent, rng, this, otherDir, level);
                Second = height - size <= maxRoomSize.h ? new BSPRoom(width, height - size, minRoomSize, maxRoomSize, fullSizedRoomPercent, rng, this) : new BSP(width, height - size, minRoomSize, maxRoomSize, fullSizedRoomPercent, rng, this, otherDir, level);
                break;
            default:
                throw new UnreachableException();
        };
        Corridor = new(this, rng);
    }

    static (BSPSplitDirection dir, BSPSplitDirection? other, int size) GetSplitDirection(Random rng, int width, int height, (int w, int h) minRoomSize, (int w, int h) maxRoomSize, BSPSplitDirection? otherDirection)
    {
        var w = CanSplit(minRoomSize.w, width, maxRoomSize.w, rng);
        var h = CanSplit(minRoomSize.h, height, maxRoomSize.h, rng);
        if (!w && !h)
            throw new UnreachableException();

        var dir = (stackalloc BSPSplitDirection[1]);
        rng.GetItems([
            // ..otherDirection is {} other ? [other] : ReadOnlySpan<SplitDirection>.Empty,
            ..w ? [BSPSplitDirection.Horizontal] : ReadOnlySpan<BSPSplitDirection>.Empty,
            ..h ? [BSPSplitDirection.Vertical] : ReadOnlySpan<BSPSplitDirection>.Empty
        ], dir);

        return dir[0] switch
        {
            BSPSplitDirection.Horizontal => (BSPSplitDirection.Horizontal, h ? BSPSplitDirection.Vertical : null, rng.Next(minRoomSize.w, width - minRoomSize.w)),
            BSPSplitDirection.Vertical => (BSPSplitDirection.Vertical, w ? BSPSplitDirection.Horizontal : null, rng.Next(minRoomSize.h, height - minRoomSize.h)),
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
        if (IsInCorridor(this, Corridor, x, y))
            return Corridor.GetTile(0, 0);
        switch (SplitDirection)
        {
            case BSPSplitDirection.Horizontal:
                if (x < SplitDistance)
                    return First.GetTile(x, y);
                return Second.GetTile(x - SplitDistance, y);
            case BSPSplitDirection.Vertical:
                if (y < SplitDistance)
                    return First.GetTile(x, y);
                return Second.GetTile(x, y - SplitDistance);
            default:
                throw new UnreachableException();
        }

        static bool IsInCorridor(BSP bsp, BSPCorridor corridor, int x, int y)
        {
            if (corridor is null)
                return false;
            (x, y) = bsp.GetPosRelativeTo(corridor, x, y);
            return 0 <= x && x < corridor.Width && 0 <= y && y < corridor.Height;
        }
    }
}

public enum BSPSplitDirection
{
    Horizontal,
    Vertical,
}

public sealed class BSPRoom : BSPNode
{
    public BSPRoom(int width, int height, (int w, int h) minRoomSize, (int w, int h) maxRoomSize, double fullSizedRoomPercent, Random rng, BSP parent)
    {
        Width = width;
        Height = height;
        Parent = parent;
        Root.Rooms.Add(this);
        var isFullSizedRoom = rng.NextBool(fullSizedRoomPercent);
        if (minRoomSize.w < width - 2 && !isFullSizedRoom)
        {
            width = rng.Next(minRoomSize.w, width - 2);
            LeftWallWidth = rng.Next(1, Width - width);
            RightWallWidth = Width - width - LeftWallWidth;
        }
        else
            LeftWallWidth = RightWallWidth = 1;

        if (minRoomSize.h < height - 2 && !isFullSizedRoom)
        {
            height = rng.Next(minRoomSize.h, height - 2);
            TopWallWidth = rng.Next(1, Height - height);
            BottomWallWidth = Height - height - TopWallWidth;
        }
        else
            TopWallWidth = BottomWallWidth = 1;
    }

    public override BSP Parent { get; }
    public override int Width { get; }
    public override int Height { get; }
    public int TopWallWidth { get; } = 1;
    public int BottomWallWidth { get; } = 1;
    public int LeftWallWidth { get; } = 1;
    public int RightWallWidth { get; } = 1;
    private readonly List<Feature> _features = [];

    public override TileTemplate GetTile(int x, int y)
    {
        ArgumentOutOfRangeException.ThrowIfNotInBetween(x, 0, Width - 1);
        ArgumentOutOfRangeException.ThrowIfNotInBetween(y, 0, Height - 1);
        if (_features.Find(f => f.X <= x && x < f.X + f.Width && f.Y <= y && y < f.Y + f.Height) is Feature f)
            return f.Template;
        if (x < LeftWallWidth || y < TopWallWidth || x >= Width - RightWallWidth || y >= Height - BottomWallWidth)
            return TileTemplate.Wall;
        return TileTemplate.Ground;
    }

    public BSPRoom? LeftRoom(int y)
    {
        foreach (var room in Root.Rooms)
        {
            (var x, y) = GetPosRelativeTo(room, 0, y);
            if (x == room.Width && y >= 0 && y < room.Height)
                return room;
        }
        return null;
    }

    public BSPRoom? RightRoom(int y)
    {
        foreach (var room in Root.Rooms)
        {
            (var x, y) = GetPosRelativeTo(room, Width, y);
            if (x == 0 && y >= 0 && y < room.Height)
                return room;
        }
        return null;
    }

    public BSPRoom? UpRoom(int x)
    {
        foreach (var room in Root.Rooms)
        {
            (x, var y) = GetPosRelativeTo(room, x, 0);
            if (y == room.Height && x >= 0 && x < room.Width)
                return room;
        }
        return null;
    }

    public BSPRoom? DownRoom(int x)
    {
        foreach (var room in Root.Rooms)
        {
            (x, var y) = GetPosRelativeTo(room, x, Height);
            if (y == 0 && x >= 0 && x < room.Width)
                return room;
        }
        return null;
    }

    public BSPRoom OtherRoom
    => (BSPRoom) OtherSide!;

    public void GenerateEntrance(Random rng)
    {
        _features.Add(new(rng.Next(LeftWallWidth, Width - (LeftWallWidth + RightWallWidth) + LeftWallWidth),
            rng.Next(TopWallWidth, Height - (TopWallWidth + BottomWallWidth) + TopWallWidth),
            1, 1, new UpstairTemplate(Parent.Level.Depth > 0)));
    }

    public void GenerateExit(Random rng)
    {
        _features.Add(new(rng.Next(LeftWallWidth, Width - (LeftWallWidth + RightWallWidth) + LeftWallWidth),
            rng.Next(TopWallWidth, Height - (TopWallWidth + BottomWallWidth) + TopWallWidth),
            1, 1, TileTemplate.Downstair));
    }

    private sealed record class Feature(int X, int Y, int Width, int Height, TileTemplate Template);
}

public sealed class BSPCorridor : BSPNode
{
    public override BSP Parent { get; }
    public override int Width { get; }
    public override int Height { get; }
    public int Position { get; }
    public int LengthOffset { get; }

    public BSPCorridor(BSP parent, Random rng)
    {
        Parent = parent;
        Width = 1;
        Height = 1;
        LengthOffset = 1;
        switch (parent.SplitDirection, parent.First, parent.Second)
        {
            case (BSPSplitDirection.Horizontal, BSPRoom left, BSPRoom right):
            {
                Width = 2;
                var top = Math.Max(1, left.TopWallWidth - 1, right.TopWallWidth - 1);
                var bot = Math.Min(parent.Height - 2, left.Height - left.BottomWallWidth, right.Height - right.BottomWallWidth);
                Position = rng.Next(top, bot + 1); // +1 because maxValue is exclusive but it should be included
                break;
            }
            case (BSPSplitDirection.Horizontal, BSPRoom left, BSP):
            {
                Width = 2;
                var top = Math.Max(1, left.TopWallWidth - 1);
                var bot = Math.Min(parent.Height - 2, left.Height - left.BottomWallWidth);
                Position = rng.Next(top, bot + 1); // +1 because maxValue is exclusive but it should be included
                break;
            }
            case (BSPSplitDirection.Horizontal, BSP, BSPRoom right):
            {
                Width = 2;
                var top = Math.Max(1, right.TopWallWidth - 1);
                var bot = Math.Min(parent.Height - 2, right.Height - right.BottomWallWidth);
                Position = rng.Next(top, bot + 1); // +1 because maxValue is exclusive but it should be included
                break;
            }
            case (BSPSplitDirection.Horizontal, BSP, BSP):
            {
                Width = 2;
                var top = 1;
                var bot = parent.Height - 2;
                Position = rng.Next(top, bot + 1); // +1 because maxValue is exclusive but it should be included
                break;
            }
            case (BSPSplitDirection.Vertical, BSPRoom top, BSPRoom bottom):
            {
                Height = 2;
                var left = Math.Max(1, top.LeftWallWidth - 1, bottom.LeftWallWidth - 1);
                var right = Math.Min(parent.Width - 2, top.Width - top.RightWallWidth, bottom.Width - bottom.RightWallWidth);
                Position = rng.Next(left, right + 1); // +1 because maxValue is exclusive but it should be included
                break;
            }
            case (BSPSplitDirection.Vertical, BSPRoom top, BSP):
            {
                Height = 2;
                var left = Math.Max(1, top.LeftWallWidth - 1);
                var right = Math.Min(parent.Width - 2, top.Width - top.RightWallWidth);
                Position = rng.Next(left, right + 1); // +1 because maxValue is exclusive but it should be included
                break;
            }
            case (BSPSplitDirection.Vertical, BSP, BSPRoom bottom):
            {
                Height = 2;
                var left = Math.Max(1, bottom.LeftWallWidth - 1);
                var right = Math.Min(parent.Width - 2, bottom.Width - bottom.RightWallWidth);
                Position = rng.Next(left, right + 1); // +1 because maxValue is exclusive but it should be included
                break;
            }
            case (BSPSplitDirection.Vertical, BSP, BSP):
            {
                Height = 2;
                var left = 1;
                var right = parent.Width - 2;
                Position = rng.Next(left, right + 1); // +1 because maxValue is exclusive but it should be included
                break;
            }
            default:
                throw new UnreachableException();
        }

        switch (parent.SplitDirection)
        {
            case BSPSplitDirection.Horizontal:
            {
                var (x, y) = GetPosRelativeTo(Parent, 1, 0);
                while (!(Parent.GetTile(x - 1, y).IsWalkable || Parent.GetTile(x, y - 1).IsWalkable || Parent.GetTile(x, y + 1).IsWalkable))
                {
                    x++;
                    Width++;
                }
                // 0, 0 after the other end because the origin won't change
                (x, y) = GetPosRelativeTo(Parent, 0, 0);
                while (!(Parent.GetTile(x - 1, y).IsWalkable || Parent.GetTile(x, y - 1).IsWalkable || Parent.GetTile(x, y + 1).IsWalkable))
                {
                    x--;
                    Width++;
                    LengthOffset++;
                }
                break;
            }
            case BSPSplitDirection.Vertical:
            {
                var (x, y) = GetPosRelativeTo(Parent, 0, 1);
                while (!(Parent.GetTile(x, y - 1).IsWalkable || Parent.GetTile(x - 1, y).IsWalkable || Parent.GetTile(x + 1, y).IsWalkable))
                {
                    y++;
                    Height++;
                }
                // 0, 0 after the other end because the origin won't change
                (x, y) = GetPosRelativeTo(Parent, 0, 0);
                while (!(Parent.GetTile(x, y - 1).IsWalkable || Parent.GetTile(x - 1, y).IsWalkable || Parent.GetTile(x + 1, y).IsWalkable))
                {
                    y--;
                    Height++;
                    LengthOffset++;
                }
                break;
            }
            default:
                throw new UnreachableException();
        }
    }

    public override TileTemplate GetTile(int x, int y)
    => TileTemplate.Ground;
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

    public BSPNode? OtherSide
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
                    case BSPSplitDirection.Horizontal:
                        x += Parent.SplitDistance;
                        break;
                    case BSPSplitDirection.Vertical:
                        y += Parent.SplitDistance;
                        break;
                    default:
                        throw new UnreachableException();
                }
            return this is BSPCorridor corridor ? GetPosInCorridor(corridor, x, y) : (x, y);
        }
        return (0, 0);

        static (int x, int y) GetPosInCorridor(BSPCorridor corridor, int x, int y)
        => corridor.Parent.SplitDirection switch
        {
            BSPSplitDirection.Horizontal => (x + corridor.Parent.SplitDistance - corridor.LengthOffset, y + corridor.Position - 1),
            BSPSplitDirection.Vertical => (x + corridor.Position - 1, y + corridor.Parent.SplitDistance - corridor.LengthOffset),
            _ => throw new UnreachableException(),
        };
    }

    public (int x, int y) GetPosRelativeTo(BSPNode other, int x, int y)
    {
        // tasks/20261024-225707-multiple-generators/tasks/20261025-164140-bsp/RelativePos.excalidraw
        if (other == this)
            return (x, y);

        return (x, y) + GetGlobalOrigin() - other.GetGlobalOrigin();
    }

    public BSPRoom GetRoomAtPos(int x, int y)
    {
        (x, y) = GetGlobalOrigin() + (x, y);
        var node = Root;
        while (true)
            switch (node.SplitDirection)
            {
                case BSPSplitDirection.Horizontal:
                    switch (x < node.SplitDistance ? node.First : node.Second)
                    {
                        case BSPRoom room:
                            return room;
                        case BSP bsp:
                            node = bsp;
                            break;
                    }
                    break;
                case BSPSplitDirection.Vertical:
                    switch (y < node.SplitDistance ? node.First : node.Second)
                    {
                        case BSPRoom room:
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
