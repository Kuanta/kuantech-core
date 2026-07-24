namespace Kuantech.Inventory
{
    /// <summary>
    /// Implemented by an item component that provides a multiplicative scale for the item's magnitudes
    /// (typically rarity-based scaling). <see cref="Item.GetScale"/> returns the first provider's scale, or
    /// 1 when none is present. This lets Core components (StatAdder, ...) scale by rarity without depending
    /// on any game-specific component type — a game supplies its own scale component and opts in here.
    /// </summary>
    public interface IItemScaleProvider
    {
        float GetScale();
    }
}
