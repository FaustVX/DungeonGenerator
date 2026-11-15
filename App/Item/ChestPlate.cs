using DungeonGenerator.App.Creature;

namespace DungeonGenerator.App.Item;

public partial record class ChestPlate(Materials.Material Material) : Item($"{Material.Name} Chest plate", ']', Material.Color), IEquipItem, IArmorItem, IInfoItem, IDurabilityItem
{
    public static EquipmentType EquipmentType => EquipmentType.Armor;
    public int Protection => 1;
    public int Durability { get; set; } = (int)(50 * Material.Hardness);

    static IEnumerable<IInfoItem.InfoMessage> IInfoItem.GetInfo(Game game, Item item)
    => [
        new(game, item, $"Equipable in {EquipmentType} slot"),
        new(game, item, $"Protection: {((IArmorItem)item).Protection}"),
        new(game, item, $"Durability: {((IDurabilityItem)item).Durability}"),
        ];
}
