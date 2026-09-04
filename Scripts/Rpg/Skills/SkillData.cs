using System;
using System.Collections.Generic;
using Kuantech.Rpg.Skills;

namespace Kuantech.Rpg
{
    /// <summary>
    /// Serializable data that represents a skill/passive skill/perk for balancing.
    /// Holds the variables and config values. Skill behaviour must still be implemented with SkillDataAsset.
    /// </summary>
    [Serializable]
    public struct SkillData
    {
        public string SkillId;
        public string SkillName;
        public string SkillDescription;
        public List<SkillVariableData> VariableDatas;
    }

    /// <summary>
    /// One utility-scaled perk variable's rank curve. Not a SkillVariableData -- a utility's own (gem-bought)
    /// rank scales BOTH BaseValue and ValuePerRank as separate LeveledValueFloat curves, a genuinely
    /// different shape from a skill/perk's flat BaseValue+ValuePerRank (see
    /// UtilityProgressibleDataAsset.ScaledPerkVariable).
    /// </summary>
    [Serializable]
    public struct UtilityVariableData
    {
        public string PerkVariableId;
        public float BaseValueAtRank0;
        public float BaseValuePerLevel;
        public float ValuePerRankAtRank0;
        public float ValuePerRankPerLevel;
    }

    [Serializable]
    public struct UtilityData
    {
        public string UtilityId;
        public List<UtilityVariableData> VariableDatas;
    }

    /// <summary>
    /// Top-level shape of Skills.json. A flat list per owner type (not a Dictionary -- JsonUtility, used by
    /// JsonDataManager, can't deserialize dictionaries), matched to assets by id at load time.
    /// </summary>
    [Serializable]
    public class SkillDataCollection
    {
        public List<SkillData> Skills;
        public List<SkillData> PassiveSkills;
        public List<SkillData> Perks;
        public List<UtilityData> Utilities;
    }
}
