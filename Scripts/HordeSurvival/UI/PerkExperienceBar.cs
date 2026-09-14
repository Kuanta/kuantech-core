using Kuantech.Core;
using Kuantech.Core.UI;
using Kuantech.HordeBonkers;
using Kuantech.Rpg;
using TMPro;
using UnityEngine;

namespace Kuantech.HordeBonkers
{
    /// <summary>
    /// The in-run perk XP bar. Reflects the run's perk LevelVariable: fills as orbs grant XP, and on a
    /// level-up fills to full, resets, then fills to the new amount. Driven by the run handler's events.
    /// </summary>
    public class PerkExperienceBar : UIElement
    {
        [SerializeField] private Fillbar Fillbar;
        [SerializeField] private TMP_Text LevelText;
        [Tooltip("How long the bar tweens toward the new fill. 0 = instant.")]
        [SerializeField] private float FillAnimateDuration = 0.3f;

        private HordeSurvivalRunHandler _runHandler;

        public override void Initialize()
        {
            base.Initialize();

            _runHandler = SubManager.GetContext<HordeSurvivalRunHandler>();
            if (_runHandler == null) return;

            _runHandler.OnPerkXpChanged -= OnPerkXpChanged;
            _runHandler.OnPerkXpChanged += OnPerkXpChanged;
            _runHandler.OnRunStarted -= OnRunStarted;
            _runHandler.OnRunStarted += OnRunStarted;

            SetExperience(false); // reflect whatever state exists right now
        }

        private void OnRunStarted() => SetExperience(false);
        private void OnPerkXpChanged() => SetExperience(true);

        public void SetExperience(bool animate = true)
        {
            if (_runHandler == null || _runHandler.RunState == null || Fillbar == null) return;

            LevelVariable xp = _runHandler.RunState.PerkXp;
            float target = xp.GetCurrentProgressPercentage();

            if (LevelText != null) LevelText.text = xp.CurrentLevel.ToString();

            // Fillbar owns the tweens now. Wrap-aware so a level-up fills to full, resets, then refills.
            if (animate) Fillbar.AnimateFillWrap(target, FillAnimateDuration);
            else Fillbar.SetFill(target);
        }

        private void OnDestroy()
        {
            if (_runHandler != null)
            {
                _runHandler.OnPerkXpChanged -= OnPerkXpChanged;
                _runHandler.OnRunStarted -= OnRunStarted;
            }
        }
    }
}
