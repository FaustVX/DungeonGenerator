using System.Collections.Frozen;
using DungeonGenerator.Creature;
using DungeonGenerator.Generator;
using DungeonGenerator.Helpers;
using DungeonGenerator.Lib;
using DungeonGenerator.Tile;

namespace DungeonGenerator;

public sealed class Level
{
    private readonly TileTemplate[,] _tiles;
    private readonly HashSet<(int x, int y)> _visibleTiles;
    private readonly HashSet<(int x, int y)> _alreadySeenTiles;
    private readonly DefaultableDictionary<(int x, int y), DefaultableDictionary<Item.Item, int>> _items = [with(capacity: 0, _ => [with(capacity:0, _ => 0)])];
    public Dictionary<MainTile, (int x, int y)> MainTiles { get; } = [];
    public Game Game { get; }
    public int Depth { get; }
    private readonly Random _rng;

    public List<Mob> Creatures { get; }

    public Level(Game game, int depth)
    {
        Game = game;
        Depth = depth;
        _rng = new Random(game.Seed + depth);
        var generator = Random.Shared.NextItem(IGenerator.Generators)(game.Width, game.Height, _rng, this);
        _tiles = new TileTemplate[game.Width, game.Height];
        _visibleTiles = [];
        _alreadySeenTiles = [];
        for (int x = 0; x < game.Width; x++)
            for (int y = 0; y < game.Height; y++)
            {
                var template = generator.GetTile(x, y);
                _tiles[x, y] = template;
                if (template is IStairTemplate { Direction: IStairTemplate.StairDirection.Up })
                    if (MainTiles.ContainsKey(MainTile.Entrance))
                        throw new Exception();
                    else
                        MainTiles.Add(MainTile.Entrance, (x, y));
                else if (template is IStairTemplate { Direction: IStairTemplate.StairDirection.Down })
                    if (MainTiles.ContainsKey(MainTile.Exit))
                        throw new Exception();
                    else
                        MainTiles.Add(MainTile.Exit, (x, y));
            }
        if (!MainTiles.ContainsKey(MainTile.Entrance) || !MainTiles.ContainsKey(MainTile.Exit))
            throw new Exception();
        if (generator is Generator.BSP.Generator gen)
        {
            Creatures = [with(capacity: Depth >= 1 ? _rng.Next(1, 5) : 1)];
            for (var i = Creatures.Capacity - 1; i >= 0; i--)
            {
                var room = _rng.NextItem(gen.Rooms);
                var (x, y) = (0, 0);
                do
                {
                    (x, y) = room.GetGlobalOrigin();
                    x = _rng.Next(x + 1, x + room.Width - 1);
                    y = _rng.Next(y + 1, y + room.Height - 1);
                }
                while (!_tiles[x, y].IsWalkable);
                Creatures.Add(new Rat(x, y));
            }
        }
        else
            Creatures = [with(capacity: 0)];
    }

    public Tile.Tile this[int x, int y]
    {
        get => new(x, y, _tiles[x, y], _visibleTiles.Contains((x, y)) && _items.TryGetValue((x, y), out var stack) ? stack : FrozenDictionary<Item.Item, int>.Empty, _visibleTiles.Contains((x, y)), _alreadySeenTiles.Contains((x, y)));
        set
        {
            _tiles[x, y] = value.Template;
            if (value.IsVisible)
            {
                foreach (var mob in Creatures)
                    if ((mob.X, mob.Y) == (x, y))
                        mob.Saw(Game);
                _visibleTiles.Add((x, y));
                _alreadySeenTiles.Add((x, y));
            }
        }
    }

    public void DropItem(int x, int y, Item.Item item, int qty, Creature.Creature dropper)
    {
        _items[(x, y)][item] += qty;
        Game.Messages.Add(new Item.Item.DropMessage(Game, dropper, qty, item));
    }

    public int PickupItem(int x, int y, Item.Item item)
    {
        var qty = _items[(x, y)][item];
        _items[(x, y)].Remove(item);
        return qty;
    }

    public void UpdateLight()
    {
        _visibleTiles.Clear();
        ShadowCaster.ComputeFieldOfViewWithShadowCasting(Game.Player.X, Game.Player.Y, Game.Player.ViewDistance,
            (x, y) => !_tiles[x, y].IsTransparent, (x, y) =>
            {
                foreach (var mob in Creatures)
                    if ((mob.X, mob.Y) == (x, y))
                        mob.Saw(Game);
                _visibleTiles.Add((x, y));
                _alreadySeenTiles.Add((x, y));
            });

        for (int x = 0; x < Game.Width; x++)
            for (int y = 0; y < Game.Height; y++)
                if (_tiles[x, y] is IUpdateableTemplate exec)
                    exec.OnUpdate(Game, x, y);
    }

    public void Update()
    {
        UpdateLight();

        foreach (var creature in Creatures)
            creature.Update(this);
    }

    public enum MainTile
    {
        Entrance,
        Exit,
    }
}
