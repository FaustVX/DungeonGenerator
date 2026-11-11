using DungeonGenerator.Helpers;
using Spectre.Console;

namespace DungeonGenerator.Creature;

public record class Rat(int X, int Y) : Mob(X, Y, "Rat", 'r', 5, Color.RosyBrown)
{
    private readonly Random _rng = new();

    public override int Force => 1;

    public override void Death(Game game)
    {
        game.CurrentLevel.DropItem(X, Y, Item.Meat.Instance, 1, this);
        base.Death(game);
    }

    public override void Update(Level level)
    {
        var (x, y) = RandomMovement(level);
        if ((level.Game.Player.X, level.Game.Player.Y) != (x, y))
            (X, Y) = (x, y);
        else
            Attack(level.Game.Player, level.Game);
    }

    private (int x, int y) RandomMovement(Level level)
    {
        int x, y;
        do
        {
            x = _rng.NextItem([-1, 0, 1]);
            y = _rng.NextItem([-1, 0, 1]);
        } while ((x, y) is not ((0, not 0) or (not 0, 0)) || !level[X + x, Y + y].Template.IsWalkable);
        return (X + x, Y + y);
    }
}
