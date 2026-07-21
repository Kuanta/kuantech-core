using System;
using Kuantech.Rpg.Skills;
using UnityEngine;

namespace Kuantech.Rpg
{
    /// <summary>
    /// Config for a stat-modifying perk: which stat to modify and how, but NOT the numbers. The actual
    /// values live in the owning PerkAsset's PerkVariables (looked up by <see cref="ValueVariableName"/>),
    /// so the effect and the description always read from the same source and can't drift apart.
    /// </summary>
    [Serializable]
    public class StatModifierPerkConfig : PerkConfig
    {
        [Tooltip("The stat this perk modifies.")]
        public AttributeAsset Stat;

        [Tooltip("Tag used to identify/group this modifier on the stats module.")]
        public string ModifierTag;

        [Tooltip("How the value is applied: flat, additive percent or multiplicative percent.")]
        public ModifierTypes ModifierType = ModifierTypes.Flat;

        [Tooltip("Name of the PerkVariable (on this PerkAsset) holding the numbers — its BaseValue and " +
                 "ValuePerRank drive the modifier. Same variable the description prints, so the shown " +
                 "number is always the applied number.")]
        public string ValueVariableName;
    }

    /// <summary>
    /// A perk that adds a StatModifier to the owner's StatsModule. Because Apply runs on acquire AND on
    /// every rank-up, it swaps the old modifier for a fresh one at the current rank (Level = CurrentRank),
    /// so the bonus scales up as the perk ranks. Remove() takes it back off (run reset).
    /// </summary>
    public class StatModifierPerk : Perk
    {
        private StatModifier _modifier;

        public override void Apply()
        {
            if (Owner == null) return;
            StatsModule stats = Owner.GetModule<StatsModule>();
            if (stats == null) return;

            // Rank-up refresh: drop the previous modifier, then add a fresh one scaled to the current rank.
            if (_modifier != null) stats.RemoveModifier(_modifier);
            _modifier = null;

            if (PerkAsset == null || PerkAsset.PerkConfig is not StatModifierPerkConfig config) return;

            if (config.Stat == null)
            {
                Debug.LogWarning($"StatModifierPerk ({PerkAsset.name}): no Stat set on the config — nothing to modify.");
                return;
            }

            SkillVariableData variable = PerkAsset.GetPerkVariable(config.ValueVariableName);
            if (variable == null)
            {
                Debug.LogWarning($"StatModifierPerk ({PerkAsset.name}): no PerkVariable named '{config.ValueVariableName}' — perk has no effect.");
                return;
            }

            _modifier = new StatModifier
            {
                AttributeAsset = config.Stat,
                ModifierTag = config.ModifierTag,
                ModifierType = config.ModifierType,
                BaseValue = variable.BaseValue,
                // Mirror the variable's display rule exactly: a display-only-base variable shows the same
                // number at every rank, so the modifier must not scale either.
                LevelToValueFactor = variable.DisplayOnlyBaseValue ? 0f : variable.ValuePerRank,
                Level = CurrentRank,
            };
            stats.AddModifier(_modifier);
        }

        public override void Remove()
        {
            if (_modifier == null || Owner == null) return;
            StatsModule stats = Owner.GetModule<StatsModule>();
            if (stats != null) stats.RemoveModifier(_modifier);
            _modifier = null;
        }
    }
}
