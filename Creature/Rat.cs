using DungeonGenerator.Helpers;
using Spectre.Console;

namespace DungeonGenerator.Creature;

public record class Rat(int X, int Y) : Mob(X, Y, "Rat", 'r', 5, Color.RosyBrown)
{
    private readonly Random _rng = new();
    private List<(int x, int y)> _path = [];

    public override int Force => 1;

    public override void Death(Game game)
    {
        game.CurrentLevel.DropItem(X, Y, Item.Meat.Instance, 1, this);
        base.Death(game);
    }

    public override void Update(Level level)
    {
        while (_path.Count <= 0 || !level[_path[0].x, _path[0].y].Template.IsWalkable)
            RandomMovement();
        if ((level.Game.Player.X, level.Game.Player.Y) != _path[0])
            ((X, Y), _path) = (_path[0], _path[1..]);
        else
            Attack(level.Game.Player, level.Game);
    }

    private void RandomMovement()
    {
        if (HP >= MaxHP && _rng.NextBool())
            _path = [..Enumerable.Repeat((X, Y), _rng.Next(1, 5))];
        else
        {
            var polar = new Polar(_rng.NextDouble() * (Math.PI * 2), _rng.Next(2, 7));
            var (x, y) = polar.ToCartesian + (X, Y);
            _path = [..Ext.Line(X, Y, x, y).Skip(1)];
        }
    }
}
