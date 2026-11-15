using DungeonGenerator.App.Creature;

namespace DungeonGenerator.App.Item;

public partial record class Dagger(Materials.Material Material) : Item($"{Material.Name} Dagger", '⚔', Material.Color), IEquipItem, IWeaponItem, IInfoItem, IDurabilityItem
{
    public static EquipmentType EquipmentType => EquipmentType.Sword;
    public int Force => 1;
    public int Durability { get; set; } = (int)(50 * Material.Hardness);
    public override string Name => $"{base.Name} ({Durability})";

    static IInfoItem.InfoMessage IInfoItem.GetInfo(Game game, Item item)
    => new(game, item, $"Force: {((IWeaponItem)item).Force}");
}
