using DungeonGenerator.Creature;
using DungeonGenerator.Lib;

namespace DungeonGenerator.Item;

public record class ChestPlate(Materials.Material Material) : Item($"{Material.Name} Chest plate", ']', Material.Color), IEquipItem, IArmorItem, IDurabilityItem
{
    public override string Name => $"{base.Name} ({Durability})";
    public static EquipmentType EquipmentType => EquipmentType.Armor;
    public int Protection => 1;
    public int Durability { get; set; } = (int)(50 * Material.Hardness);
}
