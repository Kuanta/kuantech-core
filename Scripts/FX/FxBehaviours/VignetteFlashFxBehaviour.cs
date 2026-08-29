using Kuantech.Core.FX;
using Kuantech.Core.UI;
using UnityEngine;

namespace Kuantech.Core
{
    /// <summary>
    /// Classic "damage taken" screen feedback. Finds a panel by PanelId via UIManager.GetPanelById and, if it
    /// implements IFlashPanel (e.g. DamageVignettePanel), pulses it. Lives as an FxBehaviour on whatever
    /// Effect PlayExistingEffectById(DamageReceiveEffectId, ...) plays -- reuses the existing hit-fx pipeline,
    /// no separate damage listener needed.
    ///
    /// Drives a UI overlay rather than the post-process Vignette (see ChromaticAbberationEffect for that
    /// style): a UI Image always renders regardless of post-processing quality tier (relevant on mobile), and
    /// doesn't fight a separate persistent low-health vignette that might also be driving the same
    /// VolumeComponent.
    /// </summary>
    public class VignetteFlashFxBehaviour : FxBehaviour
    {
        [Tooltip("PanelId of the IFlashPanel to trigger, looked up via UIManager.GetPanelById.")]
        public string PanelId = "DamageVignette";
        [Range(0f, 1f)] public float TargetAlpha = 0.5f;
        public float Duration = 0.4f;

        protected override void OnFxStarted(Effect parentFx)
        {
            base.OnFxStarted(parentFx);

            KtUIPanel panel = UIManager.GetPanelById(PanelId);
            if (panel is not IFlashPanel flashPanel)
            {
                Debug.LogWarning($"[VignetteFlashFxBehaviour] No IFlashPanel registered under panel id '{PanelId}'.");
                return;
            }

            flashPanel.PlayDamageFlash(TargetAlpha, Duration);
        }
    }
}
