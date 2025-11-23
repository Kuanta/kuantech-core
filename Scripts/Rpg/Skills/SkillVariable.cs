using System;

namespace Kuantech.Rpg.Skills
{
    [Serializable]
    public class SkillVariableData
    {
        public string VariableId;
        public string VariableName;
        public float BaseValue;
        public float ValuePerRank;
        public AttributeAsset AttributeToScaleWith;
        public float AttributeScalingFactor;
        public bool UsedForDPS; //To calculate dps
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
            return SkillVariableData.BaseValue + SkillVariableData.ValuePerRank * rank
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