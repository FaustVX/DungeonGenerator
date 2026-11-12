using DungeonGenerator.Creature;
using DungeonGenerator.Lib;

namespace DungeonGenerator.Item;

public record class Dagger(Materials.Material Material) : Item($"{Material.Name} Dagger", '⚔', Material.Color), IEquipItem, IWeaponItem
{
    public static EquipmentType EquipmentType => EquipmentType.Sword;
    public int Force => 1;
    public static IDictionary<Materials.Material, Dagger> Cache { get; } = new DefaultableDictionary<Materials.Material, Dagger>(m => new(m));
    public override IReadOnlyDictionary<string, Action> Actions { get; } = new Dictionary<string, Action>()
    {
        ["Equip"] = IEquipItem.Equip<Dagger>,
    };
    public IReadOnlyDictionary<string, Action> EquipedActions { get; } = new Dictionary<string, Action>()
    {
        ["Unequip"] = IEquipItem.Unequip<Dagger>,
    };
}
