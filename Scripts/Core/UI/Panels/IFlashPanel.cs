namespace Kuantech.Core.UI
{
    /// <summary>
    /// Implemented by a KtUIPanel that can play a screen-space "flash" pulse (e.g. a red damage vignette).
    /// Lets an FxBehaviour trigger it via UIManager.GetPanelById(id) without Core code depending on a
    /// game-specific panel type -- the same reasoning as Kuantech.Rpg.Skills.IArcCountConfigurable.
    /// </summary>
    public interface IFlashPanel
    {
        void PlayDamageFlash(float targetAlpha, float? duration = null);
    }
}
