using DungeonGenerator.Creature;
using Spectre.Console;

namespace DungeonGenerator.Item;

public record class Dagger() : Item("Dagger", '⚔', Color.Grey70), IEquipItem, IWeaponItem
{
    public static EquipmentType EquipmentType => EquipmentType.Sword;
    public int Force => 1;
    public static Dagger Instance => field ??= new();
    public override IReadOnlyDictionary<string, Action> Actions { get; } = new Dictionary<string, Action>()
    {
        ["Equip"] = IEquipItem.Equip<Dagger>,
    };
    public IReadOnlyDictionary<string, Action> EquipedActions { get; } = new Dictionary<string, Action>()
    {
        ["Unequip"] = IEquipItem.Unequip<Dagger>,
    };
}
