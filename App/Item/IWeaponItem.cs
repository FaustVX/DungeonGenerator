namespace DungeonGenerator.App.Item;

[InfoItem]
internal interface IWeaponItem
{
    public abstract int Force { get; }
    protected static string GetInfo(Item item)
    => $"Force: {((IWeaponItem)item).Force}";
}
