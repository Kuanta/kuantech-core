using System;
using UnityEngine;

namespace Kuantech.HordeBonkers
{
    /// <summary>
    /// Fires once the current wave has spent at least <see cref="Threshold"/> of its spawn budget — e.g. 0.5
    /// to drop the boss in halfway through the last wave, while normal enemies keep trickling around it.
    /// </summary>
    [Serializable]
    public class BudgetSpentTrigger : WaveEventTrigger
    {
        [Range(0f, 1f)]
        [Tooltip("Fraction of the wave's budget that must be spent before firing (0.5 = halfway).")]
        public float Threshold = 0.5f;

        public override bool IsMet(HordeWaveHandler handler)
        {
            int total = handler.GetWaveBudget(handler.CurrentWaveIndex);
            if (total <= 0) return true; // nothing to spend → already met
            float spent = 1f - (float)handler.RemainingBudget / total;
            return spent >= Threshold;
        }
    }
}
