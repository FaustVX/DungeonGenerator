using DungeonGenerator.Creature;
using DungeonGenerator.Lib;

namespace DungeonGenerator.Item;

public record class ChestPlate(Materials.Material Material) : Item($"{Material.Name} Chest plate", ']', Material.Color), IEquipItem, IArmorItem
{
    public static IDictionary<Materials.Material, ChestPlate> Cache { get; } = new DefaultableDictionary<Materials.Material, ChestPlate>(m => new(m));
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
