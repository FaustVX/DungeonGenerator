namespace DungeonGenerator.App.Item
{
    partial record class ChestPlate
    {
        static ChestPlate()
        {
            global::DungeonGenerator.App.Item.IEquipItem.Init<global::DungeonGenerator.App.Item.ChestPlate>();
            global::DungeonGenerator.App.Item.IInfoItem.Init<global::DungeonGenerator.App.Item.ChestPlate>();
            Init<global::DungeonGenerator.App.Item.ChestPlate>();
        }
    }
}

namespace DungeonGenerator.App.Item
{
    partial record class Dagger
    {
        static Dagger()
        {
            global::DungeonGenerator.App.Item.IEquipItem.Init<global::DungeonGenerator.App.Item.Dagger>();
            global::DungeonGenerator.App.Item.IInfoItem.Init<global::DungeonGenerator.App.Item.Dagger>();
            Init<global::DungeonGenerator.App.Item.Dagger>();
        }
    }
}

namespace DungeonGenerator.App.Item
{
    partial record class Meat
    {
        static Meat()
        {
            global::DungeonGenerator.App.Item.IEatItem.Init<global::DungeonGenerator.App.Item.Meat>();
            global::DungeonGenerator.App.Item.IInfoItem.Init<global::DungeonGenerator.App.Item.Meat>();
            Init<global::DungeonGenerator.App.Item.Meat>();
        }
    }
}

namespace DungeonGenerator.App.Item
{
    partial record class SmallTorch
    {
        static SmallTorch()
        {
            global::DungeonGenerator.App.Item.IEquipItem.Init<global::DungeonGenerator.App.Item.SmallTorch>();
            global::DungeonGenerator.App.Item.IPlaceItem.Init<global::DungeonGenerator.App.Item.SmallTorch>();
            global::DungeonGenerator.App.Item.IInfoItem.Init<global::DungeonGenerator.App.Item.SmallTorch>();
            Init<global::DungeonGenerator.App.Item.SmallTorch>();
        }
    }
}

