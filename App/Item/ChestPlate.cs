using DungeonGenerator.App.Creature;
using DungeonGenerator.App.Lib;

namespace DungeonGenerator.App.Item;

public partial record class ChestPlate(Materials.Material Material) : Item($"{Material.Name} Chest plate", ']', Material.Color), IEquipItem, IArmorItem, IDurabilityItem
{
    public override string Name => $"{base.Name} ({Durability})";
    public static EquipmentType EquipmentType => EquipmentType.Armor;
    public int Protection => 1;
    public int Durability { get; set; } = (int)(50 * Material.Hardness);
}
