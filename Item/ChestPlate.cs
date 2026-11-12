using DungeonGenerator.Creature;
using Spectre.Console;

namespace DungeonGenerator.Item;

public record class ChestPlate() : Item("Chest plate", ']', Color.Grey70), IEquipItem, IArmorItem
{
    public static ChestPlate Instance => field ??= new();
    public static EquipmentType EquipmentType => EquipmentType.Armor;
    public int Protection => 1;
    public override IReadOnlyDictionary<string, Action> Actions { get; } = new Dictionary<string, Action>()
    {
        ["Equip"] = IEquipItem.Equip<ChestPlate>,
    };
    public IReadOnlyDictionary<string, Action> EquipedActions { get; } = new Dictionary<string, Action>()
    {
        ["Unequip"] = IEquipItem.Unequip<ChestPlate>,
    };
}
