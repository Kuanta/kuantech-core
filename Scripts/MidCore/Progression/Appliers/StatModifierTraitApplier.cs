using System;
using Kuantech.Core;
using Kuantech.Rpg;

namespace Kuantech.Midcore
{
    /// <summary>
    /// The classic stat trait: adds a StatModifier scaled by the trait rank (+5% health per rank, etc.).
    /// This is the behaviour that used to live directly on TraitUpgradeProgressable, now a pluggable effect.
    /// </summary>
    [Serializable]
    public class StatModifierTraitApplier : TraitApplier
    {
        public StatModifierData ModifierData;

        public override void ApplyToActor(Actor actor, int rank)
        {
            if (actor == null || ModifierData.Stat == null) return;
            StatsModule stats = actor.GetModule<StatsModule>();
            if (stats == null) return;

            StatModifier modifier = new StatModifier(ModifierData) { Level = rank };
            stats.AddModifier(modifier);
        }

        public override string GetDescription(int rank)
        {
            if (ModifierData.Stat == null) return string.Empty;
            // Rank 0 shows the base value; higher ranks show the per-rank step, matching the old GetName.
            float value = rank == 0 ? ModifierData.BaseValue : ModifierData.LevelToValueFactor;
            return $"+{value}";
        }
    }
}
