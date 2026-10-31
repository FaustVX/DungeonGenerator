using System.Diagnostics;
using DungeonGenerator.Tile;

namespace DungeonGenerator.Generator.BSP;

public sealed class Corridor : Node
{
    public override Generator Parent { get; }
    public override int Width { get; }
    public override int Height { get; }
    public int Position { get; }
    public int LengthOffset { get; }

    public Corridor(Generator parent, Random rng)
    {
        Parent = parent;
        Width = 1;
        Height = 1;
        LengthOffset = 1;
        switch (parent.SplitDirection, parent.First, parent.Second)
        {
            case (SplitDirection.Horizontal, Room left, Room right):
            {
                Width = 2;
                var top = Math.Max(1, left.TopWallWidth - 1, right.TopWallWidth - 1);
                var bot = Math.Min(parent.Height - 2, left.Height - left.BottomWallWidth, right.Height - right.BottomWallWidth);
                Position = rng.Next(top, bot + 1); // +1 because maxValue is exclusive but it should be included
                break;
            }
            case (SplitDirection.Horizontal, Room left, Generator right):
            {
                Width = 2;
                var top = Math.Max(1, left.TopWallWidth - 1, right.TopMostWalkable() - 1);
                var bot = Math.Min(parent.Height - 2, left.Height - left.BottomWallWidth, right.BottomMostWalkable() + 1);
                Position = rng.Next(top, bot + 1); // +1 because maxValue is exclusive but it should be included
                break;
            }
            case (SplitDirection.Horizontal, Generator left, Room right):
            {
                Width = 2;
                var top = Math.Max(1, left.TopMostWalkable() - 1, right.TopWallWidth - 1);
                var bot = Math.Min(parent.Height - 2, left.BottomMostWalkable() + 1, right.Height - right.BottomWallWidth);
                Position = rng.Next(top, bot + 1); // +1 because maxValue is exclusive but it should be included
                break;
            }
            case (SplitDirection.Horizontal, Generator left, Generator right):
            {
                Width = 2;
                var top = Math.Max(1, left.TopMostWalkable() - 1, right.TopMostWalkable() - 1);
                var bot = Math.Min(parent.Height - 2, left.BottomMostWalkable() + 1, right.BottomMostWalkable() + 1);
                Position = rng.Next(top, bot + 1); // +1 because maxValue is exclusive but it should be included
                break;
            }
            case (SplitDirection.Vertical, Room top, Room bottom):
            {
                Height = 2;
                var left = Math.Max(1, top.LeftWallWidth - 1, bottom.LeftWallWidth - 1);
                var right = Math.Min(parent.Width - 2, top.Width - top.RightWallWidth, bottom.Width - bottom.RightWallWidth);
                Position = rng.Next(left, right + 1); // +1 because maxValue is exclusive but it should be included
                break;
            }
            case (SplitDirection.Vertical, Room top, Generator bot):
            {
                Height = 2;
                var left = Math.Max(1, top.LeftWallWidth - 1, bot.LeftMostWalkable() - 1);
                var right = Math.Min(parent.Width - 2, top.Width - top.RightWallWidth, bot.RightMostWalkable() + 1);
                Position = rng.Next(left, right + 1); // +1 because maxValue is exclusive but it should be included
                break;
            }
            case (SplitDirection.Vertical, Generator top, Room bottom):
            {
                Height = 2;
                var left = Math.Max(1, top.LeftMostWalkable() - 1, bottom.LeftWallWidth - 1);
                var right = Math.Min(parent.Width - 2, top.RightMostWalkable() + 1, bottom.Width - bottom.RightWallWidth);
                Position = rng.Next(left, right + 1); // +1 because maxValue is exclusive but it should be included
                break;
            }
            case (SplitDirection.Vertical, Generator top, Generator bot):
            {
                Height = 2;
                var left = Math.Max(1, top.LeftMostWalkable() - 1, bot.LeftMostWalkable() - 1);
                var right = Math.Min(parent.Width - 2, top.RightMostWalkable() + 1, bot.RightMostWalkable() + 1);
                Position = rng.Next(left, right + 1); // +1 because maxValue is exclusive but it should be included
                break;
            }
            default:
                throw new UnreachableException();
        }

        switch (parent.SplitDirection)
        {
            case SplitDirection.Horizontal:
            {
                var (x, y) = GetPosRelativeTo(Parent, 1, 0);
                while (!(Parent.GetTile(x + 1, y).IsWalkable || Parent.GetTile(x, y - 1).IsWalkable || Parent.GetTile(x, y + 1).IsWalkable))
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
            case SplitDirection.Vertical:
            {
                var (x, y) = GetPosRelativeTo(Parent, 0, 1);
                while (!(Parent.GetTile(x, y + 1).IsWalkable || Parent.GetTile(x - 1, y).IsWalkable || Parent.GetTile(x + 1, y).IsWalkable))
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
