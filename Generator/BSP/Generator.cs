using System.Diagnostics;
using DungeonGenerator.Tile;

namespace DungeonGenerator.Generator.BSP;

public class Generator : Node, IGenerator
{
    public override int Width { get; }
    public override int Height { get; }
    public ReadOnlySpan<Room> Rooms
    => (Room[])[
        ..First switch
        {
            Generator g => g.Rooms,
            Room r => [r],
            null => [],
            _ => throw new UnreachableException(),
        },
        ..Second switch
        {
            Generator g => g.Rooms,
            Room r => [r],
            null => [],
            _ => throw new UnreachableException(),
        },
    ];
    public override Generator? Parent { get; }
    public Level Level { get; }
    public Node First { get; }
    public Node Second { get; }
    public int SplitDistance { get; }
    public SplitDirection SplitDirection { get; }
    public Corridor Corridor { get; }

    public Generator? OtherBSP
    => (Generator?)OtherSide;

    public Generator(int width, int height, (int w, int h) minRoomSize, (int w, int h) maxRoomSize, double fullSizedRoomPercent, RngOrSeed random, Level level)
    : this(width, height, minRoomSize, maxRoomSize, fullSizedRoomPercent, random.Random, null, null, level)
    {
        Rooms[0].GenerateEntrance(random.Random);
        Rooms[^1].GenerateExit(random.Random);
    }

    private Generator(int width, int height, (int w, int h) minRoomSize, (int w, int h) maxRoomSize, double fullSizedRoomPercent, Random rng, Generator? parent, SplitDirection? otherDirection, Level level)
    {
        Width = width;
        Height = height;
        Parent = parent;
        Level = level;
        CheckWidthHeight(minRoomSize, maxRoomSize);
        ArgumentOutOfRangeException.ThrowIfNegative(fullSizedRoomPercent);
        switch (GetSplitDirection(rng, width, height, minRoomSize, maxRoomSize, otherDirection))
        {
            case (SplitDirection.Horizontal, var otherDir, var size):
                SplitDirection = SplitDirection.Horizontal;
                SplitDistance = size;
                First = size <= maxRoomSize.w ? new Room(size, height, minRoomSize, maxRoomSize, fullSizedRoomPercent, rng, this) : new Generator(size, height, minRoomSize, maxRoomSize, fullSizedRoomPercent, rng, this, otherDir, level);
                Second = width - size <= maxRoomSize.w ? new Room(width - size, height, minRoomSize, maxRoomSize, fullSizedRoomPercent, rng, this) : new Generator(width - size, height, minRoomSize, maxRoomSize, fullSizedRoomPercent, rng, this, otherDir, level);
                break;
            case (SplitDirection.Vertical, var otherDir, var size):
                SplitDirection = SplitDirection.Vertical;
                SplitDistance = size;
                First = size <= maxRoomSize.h ? new Room(width, size, minRoomSize, maxRoomSize, fullSizedRoomPercent, rng, this) : new Generator(width, size, minRoomSize, maxRoomSize, fullSizedRoomPercent, rng, this, otherDir, level);
                Second = height - size <= maxRoomSize.h ? new Room(width, height - size, minRoomSize, maxRoomSize, fullSizedRoomPercent, rng, this) : new Generator(width, height - size, minRoomSize, maxRoomSize, fullSizedRoomPercent, rng, this, otherDir, level);
                break;
            default:
                throw new UnreachableException();
        };
        Corridor = new(this, rng);
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

    public override TileTemplate GetTile(int x, int y)
    {
        ArgumentOutOfRangeException.ThrowIfNotInBetween(x, 0, Width - 1);
        ArgumentOutOfRangeException.ThrowIfNotInBetween(y, 0, Height - 1);
        if (IsInCorridor(this, Corridor, x, y))
            return Corridor.GetTile(0, 0);
        switch (SplitDirection)
        {
            case SplitDirection.Horizontal:
                if (x < SplitDistance)
                    return First.GetTile(x, y);
                return Second.GetTile(x - SplitDistance, y);
            case SplitDirection.Vertical:
                if (y < SplitDistance)
                    return First.GetTile(x, y);
                return Second.GetTile(x, y - SplitDistance);
            default:
                throw new UnreachableException();
        }

        static bool IsInCorridor(Generator bsp, Corridor corridor, int x, int y)
        {
            if (corridor is null)
                return false;
            (x, y) = bsp.GetPosRelativeTo(corridor, x, y);
            return 0 <= x && x < corridor.Width && 0 <= y && y < corridor.Height;
        }
    }
}
