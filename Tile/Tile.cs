namespace DungeonGenerator.Tile;

public record struct Tile(int X, int Y, TileTemplate Template, bool HasBeenSeen);
