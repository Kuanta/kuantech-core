using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Kuantech.Rpg.Skills
{
    /// <summary>
    /// A rank-scaled number plus how to show it. Used by skills AND perks — both are "values that grow
    /// with rank and get printed into a description", so they share one type rather than drifting apart.
    ///
    /// The value itself needs no runtime context: <see cref="GetValueByRank"/> works straight off the
    /// data, which is what description building uses. Attribute scaling needs a StatsModule and is
    /// resolved by <see cref="SkillVariable"/> at runtime.
    /// </summary>
    [Serializable]
    public class SkillVariableData
    {
        [Tooltip("Key used to reference this variable — the {Placeholder} name in a description.")]
        [FormerlySerializedAs("Name")]
        public string VariableId;
        public string VariableName;
        public float BaseValue;
        public float ValuePerRank;
        public AttributeAsset AttributeToScaleWith;
        public float AttributeScalingFactor;
        public bool UsedForDPS; //To calculate dps

        [Header("Display")]
        public Color TextColor = Color.white;
        [Tooltip("Show as a percentage: the value is multiplied by 100 and suffixed with '%'.")]
        public bool IsPercentage;
        [Tooltip("Always display the base value, ignoring rank — for numbers that do not grow.")]
        public bool DisplayOnlyBaseValue;

        /// <summary>Rank-scaled value, without attribute scaling (no actor context needed).</summary>
        public float GetValueByRank(int rank)
        {
            return BaseValue + ValuePerRank * rank;
        }

        /// <summary>The number to print for this rank.</summary>
        public float GetDisplayValue(int rank)
        {
            return DisplayOnlyBaseValue ? BaseValue : GetValueByRank(rank);
        }
    }
    
    /// <summary>
    /// Numeric, scalable numeric variable that can be used in skills. Skill variables also scale with attributes.
    /// </summary>
    public class SkillVariable
    {
        public SkillVariableData SkillVariableData;

        [NonSerialized] public Skill ParentSkill;

        public SkillVariable(SkillVariableData data)
        {
            SkillVariableData = data;
        }

        public float GetValue()
        {
            int rank = ParentSkill.SkillRank;
            return GetValueByRank(rank);
        }
        
        public float GetValueByRank(int rank)
        {
            return SkillVariableData.GetValueByRank(rank)
                   + SkillVariableData.AttributeScalingFactor * GetAttributeValue();
        }

        public float GetAttributeValue()
        {
            if (ParentSkill == null || ParentSkill.ParentSpellBook == null || SkillVariableData.AttributeToScaleWith == null ) return 0;
            StatsModule sm = ParentSkill.ParentSpellBook.Actor.GetModule<StatsModule>();
            if (sm == null) return 0;
            return sm.GetAttributeValue(SkillVariableData.AttributeToScaleWith);
        }
    }
}