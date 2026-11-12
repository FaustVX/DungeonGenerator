using DungeonGenerator.Helpers;
using DungeonGenerator.Tile;

namespace DungeonGenerator.Generator.BSP;

public sealed class Room : Node, IRoom
{
    public Room(int width, int height, (int w, int h) minRoomSize, (int w, int h) maxRoomSize, double fullSizedRoomPercent, Random rng, Generator parent)
    {
        Width = width;
        Height = height;
        Parent = parent;
        var isFullSizedRoom = rng.NextBool(fullSizedRoomPercent);
        if (minRoomSize.w < width - 2 && !isFullSizedRoom)
        {
            width = rng.Next(Math.Max(minRoomSize.w, width / 2 - 1), width - 2);
            LeftWallWidth = rng.Next(1, Width - width);
            RightWallWidth = Width - width - LeftWallWidth;
        }
        else
            LeftWallWidth = RightWallWidth = 1;

        if (minRoomSize.h < height - 2 && !isFullSizedRoom)
        {
            height = rng.Next(Math.Max(minRoomSize.h, height / 2 - 1), height - 2);
            TopWallWidth = rng.Next(1, Height - height);
            BottomWallWidth = Height - height - TopWallWidth;
        }
        else
            TopWallWidth = BottomWallWidth = 1;

        width = Width - LeftWallWidth - RightWallWidth;
        height = Height - TopWallWidth - BottomWallWidth;
        if (width >= 7 && height >= 7 && width % 2 == 1 && height % 2 == 1)
        {
            (int x, int y) center = (width / 2, height / 2) + (LeftWallWidth, TopWallWidth);
            _features.Add(new(center.x, center.y, TileTemplate.Wall));
            (int x, int y) torch = center + (0, 1);
            _features.Add(new(torch.x, torch.y, TileTemplate.SmallTorch(rng.NextBool(.2))));
            torch = center + (0, -1);
            _features.Add(new(torch.x, torch.y, TileTemplate.SmallTorch(rng.NextBool(.2))));
            torch = center + (1, 0);
            _features.Add(new(torch.x, torch.y, TileTemplate.SmallTorch(rng.NextBool(.2))));
            torch = center + (-1, 0);
            _features.Add(new(torch.x, torch.y, TileTemplate.SmallTorch(rng.NextBool(.2))));
        }
        (X, Y) = GetGlobalOrigin();
    }

    public override Generator Parent { get; }
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
        if (_features.Find(f => f.X == x && f.Y == y) is Feature f)
            return f.Template;
        if (x < LeftWallWidth || y < TopWallWidth || x >= Width - RightWallWidth || y >= Height - BottomWallWidth)
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

    public Room OtherRoom
    => (Room) OtherSide!;

    public int X { get; }

    public int Y { get; }

    public List<IRoom> ConnectedTo { get; } = [with(capacity: 4)];
    IReadOnlyList<IRoom> IRoom.ConnectedTo => ConnectedTo;

    public void GenerateEntrance(Random rng)
    {
        _features.Add(new(rng.Next(LeftWallWidth + 1, Width - (LeftWallWidth + RightWallWidth) + LeftWallWidth - 1),
            rng.Next(TopWallWidth + 1, Height - (TopWallWidth + BottomWallWidth) + TopWallWidth - 1),
            TileTemplate.UpStair(Parent.Level.Depth > 0)));
    }

    public void GenerateExit(Random rng)
    {
        _features.Add(new(rng.Next(LeftWallWidth + 1, Width - (LeftWallWidth + RightWallWidth) + LeftWallWidth - 1),
            rng.Next(TopWallWidth + 1, Height - (TopWallWidth + BottomWallWidth) + TopWallWidth - 1),
            TileTemplate.DownStair(false)));
    }

    private sealed record class Feature(int X, int Y, TileTemplate Template);
}
