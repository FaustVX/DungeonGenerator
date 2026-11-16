namespace DungeonGenerator.App.Item;

[InfoItem]
internal interface IArmorItem
{
    public abstract int Protection { get; }
    protected static string GetInfo(Item item)
    => $"Protection: {((IArmorItem)item).Protection}";
}
