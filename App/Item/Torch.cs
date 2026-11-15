using DungeonGenerator.App.Tile;

namespace DungeonGenerator.App.Item;

public abstract record class Torch(string Name, double Range, TileTemplate Template) : Item(Name, Template.Symbol, Template.Foreground), IEquipItem, IPlaceItem, IInfoItem, IDurabilityItem
{
    public static Creature.EquipmentType EquipmentType => Creature.EquipmentType.Torch;
    public abstract int Durability { get; set; }

    static IEnumerable<IInfoItem.InfoMessage> IInfoItem.GetInfo(Game game, Item item)
    => [
        new(game, item, IEquipItem.GetInfo<Torch>()),
        new(game, item, IPlaceItem.GetInfo(item, "Vision", t => ((ITorchTemplate)t).Range)),
        new(game, item, IDurabilityItem.GetInfo(item)),
        ];
}

public partial record class SmallTorch() : Torch("Small Torch", 3, TileTemplate.SmallTorch())
{
    public override int Durability { get; set; } = 500;
}
