using DungeonGenerator.App.Creature;
using DungeonGenerator.App.Lib;

namespace DungeonGenerator.App.Item;

public record class Dagger(Materials.Material Material) : Item($"{Material.Name} Dagger", '⚔', Material.Color), IEquipItem, IWeaponItem, IDurabilityItem
{
    public static EquipmentType EquipmentType => EquipmentType.Sword;
    public int Force => 1;
    public int Durability { get; set; } = (int)(50 * Material.Hardness);
    public override string Name => $"{base.Name} ({Durability})";
}
