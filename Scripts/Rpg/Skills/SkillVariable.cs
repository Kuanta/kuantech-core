using System;

namespace Kuantech.Rpg.Skills
{
    [Serializable]
    public struct SkillVariableData
    {
        public string VariableId;
        public string VariableName;
        public float BaseValue;
        public float ValuePerRank;
        public AttributeAsset AttributeToScaleWith;
        public float AttributeScalingFactor;
    }
    
    /// <summary>
    /// Numeric, scalable numeric variable that can be used in skills.
    /// </summary>
    public class SkillVariable
    {
        public SkillVariableData SkillVariableData;
        public int CurrentRank;

        [NonSerialized] public Skill ParentSkill;

        public SkillVariable()
        {
            CurrentRank = 0;
        }

        public SkillVariable(SkillVariableData data)
        {
            SkillVariableData = data;
        }

        public void SetRank(int rank)
        {
            CurrentRank = rank;
        }
        public float GetValue()
        {
            return GetValueByRank(CurrentRank);
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