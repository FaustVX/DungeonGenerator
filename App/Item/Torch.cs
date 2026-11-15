using DungeonGenerator.App.Tile;

namespace DungeonGenerator.App.Item;

public abstract record class Torch(string Name, double Range, TileTemplate Template) : Item(Name, Template.Symbol, Template.Foreground), IEquipItem, IPlaceItem, IInfoItem, IDurabilityItem
{
    public static Creature.EquipmentType EquipmentType => Creature.EquipmentType.Torch;
    public abstract int Durability { get; set; }

    static IEnumerable<IInfoItem.InfoMessage> IInfoItem.GetInfo(Game game, Item item)
    => [
        new(game, item, $"Equipable in {EquipmentType} slot"),
        new(game, item, $"Vision: {((ITorchTemplate)((IPlaceItem)item).Template).Range}"),
        new(game, item, $"Durability: {((IDurabilityItem)item).Durability}"),
        ];
}

public partial record class SmallTorch() : Torch("Small Torch", 3, TileTemplate.SmallTorch())
{
    public override int Durability { get; set; } = 500;
}
