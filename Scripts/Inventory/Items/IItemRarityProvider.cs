namespace Kuantech.Inventory
{
    /// <summary>
    /// Implemented by an item component that carries a rarity tier for the item. <see cref="Item.GetRarity"/>
    /// returns the first provider's rarity index, or -1 when none is present. This lets Core UI (inventory
    /// slots, ...) color by rarity without depending on any game-specific rarity component — a game supplies
    /// its own component and opts in here.
    /// </summary>
    public interface IItemRarityProvider
    {
        int GetRarity();
    }
}
