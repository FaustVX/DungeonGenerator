using System.Diagnostics;
using DungeonGenerator.Tile;

namespace DungeonGenerator.Generator;

public class BSP : IGenerator, BSP.INode
{
    public int Width { get; }
    public int Height { get; }
    private readonly int _splitDistance;
    private readonly SplitDirection _splitDirection;
    public BSP? Parent { get; }
    public INode First { get; }
    public INode Second { get; }

    public BSP(int width, int height, (int w, int h) minRoomSize, (int w, int h) maxRoomSize, double fullSizedRoomPercent, int seed)
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
        Parent = null;
        var rng = new Random(seed);
        switch (GetSplitDirection(rng, width, height, minRoomSize, maxRoomSize, null))
        {
            case (SplitDirection.Horizontal, var otherDir, var size):
                _splitDirection = SplitDirection.Horizontal;
                _splitDistance = size;
                First = new BSP(size, height, minRoomSize, maxRoomSize, fullSizedRoomPercent, rng, this, otherDir);
                Second = new BSP(width - size, height, minRoomSize, maxRoomSize, fullSizedRoomPercent, rng, this, otherDir);
                break;
            case (SplitDirection.Vertical, var otherDir, var size):
                _splitDirection = SplitDirection.Vertical;
                _splitDistance = size;
                First = new BSP(width, size, minRoomSize, maxRoomSize, fullSizedRoomPercent, rng, this, otherDir);
                Second = new BSP(width, height - size, minRoomSize, maxRoomSize, fullSizedRoomPercent, rng, this, otherDir);
                break;
            default:
                throw new UnreachableException();
        };
    }

    private BSP(int width, int height, (int w, int h) minRoomSize, (int w, int h) maxRoomSize, double fullSizedRoomPercent, Random rng, BSP parent, SplitDirection? otherDirection)
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
                First = new BSP(size, height, minRoomSize, maxRoomSize, fullSizedRoomPercent, rng, this, otherDir);
                Second = new BSP(width - size, height, minRoomSize, maxRoomSize, fullSizedRoomPercent, rng, this, otherDir);
                break;
            case (SplitDirection.Vertical, var otherDir, var size):
                _splitDirection = SplitDirection.Vertical;
                _splitDistance = size;
                First = new BSP(width, size, minRoomSize, maxRoomSize, fullSizedRoomPercent, rng, this, otherDir);
                Second = new BSP(width, height - size, minRoomSize, maxRoomSize, fullSizedRoomPercent, rng, this, otherDir);
                break;
            default:
                First = new Room() { Parent = this };
                Second = new Room() { Parent = this };
                break;
        };
    }

    static (SplitDirection dir, SplitDirection? other, int size)? GetSplitDirection(Random rng, int width, int height, (int w, int h) minRoomSize, (int w, int h) maxRoomSize, SplitDirection? otherDirection)
    {
        var w = CanSplit(minRoomSize.w, width, maxRoomSize.w, rng);
        var h = CanSplit(minRoomSize.h, height, maxRoomSize.h, rng);
        if (!w && !h)
            return null;

        var dir = (stackalloc SplitDirection[1]);
        rng.GetItems([
            ..otherDirection is {} other ? [other] : ReadOnlySpan<SplitDirection>.Empty,
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
        throw new NotImplementedException();
    }

    public interface INode
    {
        public BSP? Parent { get; }
        public TileTemplate GetTile(int x, int y);
    }

    private enum SplitDirection
    {
        Horizontal,
        Vertical,
    }

    private sealed class Room : INode
    {
        public required BSP Parent { get; init; }
        public TileTemplate GetTile(int x, int y)
        {
            throw new NotImplementedException();
        }
    }
}
