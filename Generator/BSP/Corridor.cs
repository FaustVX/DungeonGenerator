using System.Diagnostics;
using DungeonGenerator.Helpers;
using DungeonGenerator.Tile;

namespace DungeonGenerator.Generator.BSP;

public sealed class Corridor : Node, IRoom
{
    private readonly Random _rng;
    public override Generator Parent { get; }
    public override int Width { get; }
    public override int Height { get; }
    public int Position { get; }
    public int LengthOffset { get; }

    public int X { get; }

    public int Y { get; }

    public List<IRoom> ConnectedTo { get; }
    IReadOnlyList<IRoom> IRoom.ConnectedTo => ConnectedTo;

    public Corridor(Generator parent, Random rng)
    {
        Parent = parent;
        _rng = rng;
        Width = 1;
        Height = 1;
        LengthOffset = 1;
        ConnectedTo = [with(capacity: 2)];

        switch (parent.SplitDirection)
        {
            case SplitDirection.Horizontal:
            {
                Width = 2;
                var top = Math.Max(1, parent.First.TopMostWalkable() - 1, parent.Second.TopMostWalkable() - 1);
                var bot = Math.Min(parent.Height - 2, parent.First.BottomMostWalkable() + 1, parent.Second.BottomMostWalkable() + 1);
                Position = rng.Next(top, bot + 1); // +1 because maxValue is exclusive but it should be included
                var (x, y) = GetPosRelativeTo(Parent, 1, 0);
                while (!(Parent.GetTile(x + 1, y).IsWalkable || Parent.GetTile(x, y - 1).IsWalkable || Parent.GetTile(x, y + 1).IsWalkable))
                {
                    x++;
                    Width++;
                }
                foreach (var pos in (ReadOnlySpan<(int x, int y)>)[(x + 1, y), (x, y - 1), (x, y + 1)])
                    if (Parent.GetTile(pos.x, pos.y).IsWalkable)
                    {
                        var room = Parent.GetNodeAt(pos.x, pos.y);
                        room.ConnectedTo.Add(this);
                        ConnectedTo.Add(room);
                    }
                // 0, 0 after the other end because the origin won't change
                (x, y) = GetPosRelativeTo(Parent, 0, 0);
                while (!(Parent.GetTile(x - 1, y).IsWalkable || Parent.GetTile(x, y - 1).IsWalkable || Parent.GetTile(x, y + 1).IsWalkable))
                {
                    x--;
                    Width++;
                    LengthOffset++;
                }
                foreach (var pos in (ReadOnlySpan<(int x, int y)>)[(x - 1, y), (x, y - 1), (x, y + 1)])
                    if (Parent.GetTile(pos.x, pos.y).IsWalkable)
                    {
                        var room = Parent.GetNodeAt(pos.x, pos.y);
                        room.ConnectedTo.Add(this);
                        ConnectedTo.Add(room);
                    }
                break;
            }
            case SplitDirection.Vertical:
            {
                Height = 2;
                var left = Math.Max(1, parent.First.LeftMostWalkable() - 1, parent.Second.LeftMostWalkable() - 1);
                var right = Math.Min(parent.Width - 2, parent.First.RightMostWalkable() + 1, parent.Second.RightMostWalkable() + 1);
                Position = rng.Next(left, right + 1); // +1 because maxValue is exclusive but it should be included
                var (x, y) = GetPosRelativeTo(Parent, 0, 1);
                while (!(Parent.GetTile(x, y + 1).IsWalkable || Parent.GetTile(x - 1, y).IsWalkable || Parent.GetTile(x + 1, y).IsWalkable))
                {
                    y++;
                    Height++;
                }
                foreach (var pos in (ReadOnlySpan<(int x, int y)>)[(x, y + 1), (x - 1, y), (x + 1, y)])
                    if (Parent.GetTile(pos.x, pos.y).IsWalkable)
                    {
                        var room = Parent.GetNodeAt(pos.x, pos.y);
                        room.ConnectedTo.Add(this);
                        ConnectedTo.Add(room);
                    }
                // 0, 0 after the other end because the origin won't change
                (x, y) = GetPosRelativeTo(Parent, 0, 0);
                while (!(Parent.GetTile(x, y - 1).IsWalkable || Parent.GetTile(x - 1, y).IsWalkable || Parent.GetTile(x + 1, y).IsWalkable))
                {
                    y--;
                    Height++;
                    LengthOffset++;
                }
                foreach (var pos in (ReadOnlySpan<(int x, int y)>)[(x, y - 1), (x - 1, y), (x + 1, y)])
                    if (Parent.GetTile(pos.x, pos.y).IsWalkable)
                    {
                        var room = Parent.GetNodeAt(pos.x, pos.y);
                        room.ConnectedTo.Add(this);
                        ConnectedTo.Add(room);
                    }
                break;
            }
            default:
                throw new UnreachableException();
        }
        (X, Y) = GetGlobalOrigin();
    }

    public override TileTemplate GetTile(int x, int y)
    {
        switch (Parent.SplitDirection)
        {
            case SplitDirection.Horizontal:
                if (Width >= 7)
                {
                    if ((x, y) == (Width - 1, 0) || (x, y) is (0, 0))
                        return TileTemplate.SmallTorch(_rng.NextBool(.25));
                }
                else if ((x, y) == (Width / 2, 0))
                    return TileTemplate.SmallTorch(_rng.NextBool(.25));
                break;
            case SplitDirection.Vertical:
                if (Height >= 7)
                {
                    if ((x, y) == (0, Height - 1) || (x, y) is (0, 0))
                        return TileTemplate.SmallTorch(_rng.NextBool(.25));
                }
                else if ((x, y) == (0, Height / 2))
                    return TileTemplate.SmallTorch(_rng.NextBool(.25));
                break;
            default:
                throw new UnreachableException();
        }
        return TileTemplate.Ground;
    }
}
