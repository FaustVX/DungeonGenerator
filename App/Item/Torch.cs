using DungeonGenerator.App.Tile;

namespace DungeonGenerator.App.Item;

public abstract record class Torch(string Name, double Range, TileTemplate Template) : Item(Name, Template.Symbol, Template.Foreground), IEquipItem, IPlaceItem, IInfoItem, IDurabilityItem
{
    public static Creature.EquipmentType EquipmentType => Creature.EquipmentType.Torch;
    public abstract int Durability { get; set; }
    public override string Name => $"{base.Name} ({Durability})";

    static IInfoItem.InfoMessage IInfoItem.GetInfo(Game game, Item item)
    => new(game, item, $"Vision: {((ITorchTemplate)((Torch)item).Template).Range}");
}

public partial record class SmallTorch() : Torch("Small Torch", 3, TileTemplate.SmallTorch())
{
    public override int Durability { get; set; } = 500;
}
