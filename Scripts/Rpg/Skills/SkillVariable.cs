using System;
using System.Collections.Generic;
using Kuantech.Core.Database.Attributes;
using Kuantech.Rpg.Managers;
using UnityEngine;

namespace Kuantech.Rpg.Skills
{
    /// <summary>
    /// A rank-scaled number plus how to show it. Used by skills AND perks — both are "values that grow
    /// with rank and get printed into a description", so they share one type rather than drifting apart.
    ///
    /// The value itself needs no runtime context: <see cref="GetValueByRank"/> works straight off the
    /// data, which is what description building uses. Attribute scaling needs a StatsModule and is
    /// resolved by <see cref="SkillVariable"/> at runtime.
    ///
    /// [KtDatabaseVariable]-tagged fields are what SkillBalancer rebuilds from the "SkillVariables" table
    /// (see DataTable.SetVariablesFromRow). TextColor is left out on purpose: a Color doesn't map onto the
    /// KtDataType set (KtFloat/KtInt/KtString/KtBool/arrays only), and it's a display-only concern a
    /// designer is unlikely to need to retune from a spreadsheet.
    ///
    /// AttributeToScaleWith itself can't be reflection-populated either (it's an AttributeAsset reference,
    /// not a primitive) -- AttributeToScaleWithId carries the id through the table instead, and
    /// SkillBalancer resolves it to the actual asset via RpgManager.GetAttributeAssetById after the
    /// reflection pass runs.
    /// </summary>
    [Serializable]
    public class SkillVariableData
    {
        [Tooltip("Whether SkillBalancer writes/reads this variable to/from the database. Turn off for a " +
                 "fixed default that a modifier perk (e.g. ActiveSkillVariableOverridePerk) overrides -- " +
                 "the perk's own PerkVariables is where that number is actually tuned, so the skill's own " +
                 "copy is structural, not something a designer needs in the sheet. Never itself written to " +
                 "the table (it's an authoring-time choice, not a balance number).")]
        public bool Balancable = true;

        [Tooltip("Key used to reference this variable — the {Placeholder} name in a description.")]
        [KtDatabaseVariable("VariableId")]
        public string VariableId;
        [KtDatabaseVariable("VariableName")]
        public string VariableName;
        [KtDatabaseVariable("BaseValue")]
        public float BaseValue;
        [KtDatabaseVariable("ValuePerRank")]
        public float ValuePerRank;
        public AttributeAsset AttributeToScaleWith;
        [Tooltip("Id of AttributeToScaleWith, resolved via RpgManager.GetAttributeAssetById -- only used " +
                 "as the round-trip through the database table; AttributeToScaleWith is what's read at runtime.")]
        [KtDatabaseVariable("AttributeToScaleWithId")]
        public string AttributeToScaleWithId;
        [KtDatabaseVariable("AttributeScalingFactor")]
        public float AttributeScalingFactor;
        [KtDatabaseVariable("UsedForDPS")]
        public bool UsedForDPS; //To calculate dps

        [Header("Display")]
        public Color TextColor = Color.white;
        [Tooltip("Show as a percentage: the value is multiplied by 100 and suffixed with '%'.")]
        [KtDatabaseVariable("IsPercentage")]
        public bool IsPercentage;
        [Tooltip("Always display the base value, ignoring rank — for numbers that do not grow.")]
        [KtDatabaseVariable("DisplayOnlyBaseValue")]
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

        /// <summary>
        /// Rebuilds <paramref name="target"/> from <paramref name="source"/> (a skill/perk's balance data,
        /// from Skills.json or previously a DataTable row): non-Balancable entries are left untouched (a
        /// modifier perk's fixed default, never meant to be tuned from the sheet), every Balancable entry is
        /// replaced wholesale by what source currently has for that owner. Source is authoritative for WHICH
        /// variables exist, not just their values -- an id present in source but missing from target gets
        /// added, and one missing from source simply isn't recreated.
        /// </summary>
        public static void RebuildBalancable(List<SkillVariableData> target, List<SkillVariableData> source)
        {
            if (target == null) return;
            target.RemoveAll(v => v == null || v.Balancable);
            if (source == null) return;

            foreach (var incoming in source)
            {
                if (incoming == null || string.IsNullOrEmpty(incoming.VariableId)) continue;

                var variable = new SkillVariableData
                {
                    VariableId = incoming.VariableId,
                    VariableName = incoming.VariableName,
                    BaseValue = incoming.BaseValue,
                    ValuePerRank = incoming.ValuePerRank,
                    AttributeToScaleWithId = incoming.AttributeToScaleWithId,
                    AttributeScalingFactor = incoming.AttributeScalingFactor,
                    UsedForDPS = incoming.UsedForDPS,
                    IsPercentage = incoming.IsPercentage,
                    DisplayOnlyBaseValue = incoming.DisplayOnlyBaseValue,
                };

                // AttributeToScaleWith is an asset reference -- JsonUtility can't carry it, only its id, so
                // resolve it to the actual asset here (mirrors SkillBalancer's old DataTable round-trip).
                if (!string.IsNullOrEmpty(variable.AttributeToScaleWithId))
                    variable.AttributeToScaleWith = RpgManager.GetAttributeAssetById(variable.AttributeToScaleWithId);

                target.Add(variable);
            }
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