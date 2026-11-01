namespace DungeonGenerator.Tile;

public readonly record struct Tile(int X, int Y, TileTemplate Template, bool IsVisible, bool HasBeenSeen);
