
using DungeonGenerator.Helpers;
namespace DungeonGenerator.Item;

public interface IDurabilityItem
{
    public abstract int Durability { get; set; }

    public class BrokeMessage(Game Game, Item Item) : Messages.Message(Game)
    {
        public override string ToString()
        => $"{_timestamp} {ItemColor(Item)} broke";
    }

    public static void Damage(Creature.Player player, Game game, Creature.EquipmentType equipment, int damage)
    {
        if (player.Equipments.TryGetValue(equipment, out IDurabilityItem item) && (item.Durability -= damage) <= 0)
        {
            player.Equipments.Remove(equipment);
            game.Messages.Add(new BrokeMessage(game, (Item)item));
        }
    }
}
