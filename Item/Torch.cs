using DungeonGenerator.Tile;

namespace DungeonGenerator.Item;

public abstract record class Torch(string Name, double Range, TileTemplate Template) : Item(Name, Template.Symbol, Template.Foreground), IPlaceItem, IEquipItem, IDurabilityItem
{
    public static Creature.EquipmentType EquipmentType => Creature.EquipmentType.Torch;
    public abstract int Durability { get; set; }
    public override string Name => $"{base.Name} ({Durability})";

    public override IReadOnlyDictionary<string, Action> Actions { get; } = new Dictionary<string, Action>()
    {
        ["Place"] = IPlaceItem.Place,
        ["Equip"] = IEquipItem.Equip<Torch>,
    };

    public IReadOnlyDictionary<string, Action> EquipedActions { get; } = new Dictionary<string, Action>()
    {
        ["Unequip"] = IEquipItem.Unequip<Torch>,
    };
}

public record class SmallTorch() : Torch("Small Torch", 3, TileTemplate.SmallTorch())
{
    public override int Durability { get; set; } = 500;
}
