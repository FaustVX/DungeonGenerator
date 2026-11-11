using DungeonGenerator.Tile;

namespace DungeonGenerator.Item;

public abstract record class Torch(string Name, double Range, TileTemplate Template) : Item(Name, Template.Symbol, Template.Foreground), IPlaceItem, IEquipItem
{
    public static Creature.EquipmentType EquipmentType => Creature.EquipmentType.Torch;

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
    public static SmallTorch Instance => field ??= new();
}
