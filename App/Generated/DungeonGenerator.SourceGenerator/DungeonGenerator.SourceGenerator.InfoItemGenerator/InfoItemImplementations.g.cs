namespace DungeonGenerator.App.Item
{
    partial record class ChestPlate
    {
        static global::System.Collections.Generic.IEnumerable<global::DungeonGenerator.App.Item.IInfoItem.InfoMessage> global::DungeonGenerator.App.Item.IInfoItem.GetInfo(global::DungeonGenerator.App.Game game, global::DungeonGenerator.App.Item.Item item)
        =>
        [
            new(game, item, global::DungeonGenerator.App.Item.IEquipItem.GetInfo<global::DungeonGenerator.App.Item.ChestPlate>()),
            new(game, item, global::DungeonGenerator.App.Item.IArmorItem.GetInfo(item)),
            new(game, item, global::DungeonGenerator.App.Item.IDurabilityItem.GetInfo(item)),
        ];
    }
}

namespace DungeonGenerator.App.Item
{
    partial record class Dagger
    {
        static global::System.Collections.Generic.IEnumerable<global::DungeonGenerator.App.Item.IInfoItem.InfoMessage> global::DungeonGenerator.App.Item.IInfoItem.GetInfo(global::DungeonGenerator.App.Game game, global::DungeonGenerator.App.Item.Item item)
        =>
        [
            new(game, item, global::DungeonGenerator.App.Item.IEquipItem.GetInfo<global::DungeonGenerator.App.Item.Dagger>()),
            new(game, item, global::DungeonGenerator.App.Item.IWeaponItem.GetInfo(item)),
            new(game, item, global::DungeonGenerator.App.Item.IDurabilityItem.GetInfo(item)),
        ];
    }
}

namespace DungeonGenerator.App.Item
{
    partial record class Meat
    {
        static global::System.Collections.Generic.IEnumerable<global::DungeonGenerator.App.Item.IInfoItem.InfoMessage> global::DungeonGenerator.App.Item.IInfoItem.GetInfo(global::DungeonGenerator.App.Game game, global::DungeonGenerator.App.Item.Item item)
        =>
        [
            new(game, item, global::DungeonGenerator.App.Item.IEatItem.GetInfo(item)),
        ];
    }
}

