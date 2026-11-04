namespace DungeonGenerator.Tile;

public readonly record struct Tile(int X, int Y, TileTemplate Template, IReadOnlyDictionary<Item.Item, int> Items, bool IsVisible, bool HasBeenSeen);
