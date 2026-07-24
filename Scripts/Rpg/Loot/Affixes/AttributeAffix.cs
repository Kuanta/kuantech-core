using System;
using Kuantech.Core;
using Kuantech.Rpg.Managers;
using UnityEngine;

namespace Kuantech.Rpg
{
    /// <summary>
    /// Authorable definition of an attribute affix (a flat bonus to one attribute). Edit these in the
    /// inspector via a [SerializeReference] list; each produces a runtime <see cref="AttributeAffix"/>.
    /// </summary>
    [Serializable]
    public class AttributeAffixData : AffixData
    {
        public string AttributeId;
        public float BaseValue;
        public float ValuePerLevel;

        protected override Affix Instantiate()
        {
            return new AttributeAffix
            {
                AttributeId = AttributeId,
                BaseValue = BaseValue,
                ValuePerLevel = ValuePerLevel,
            };
        }
    }

    /// <summary>
    /// Runtime attribute affix: applies a flat StatModifier to one attribute, scaled by the affix's level.
    /// Item rarity does not scale affixes (it governs how many roll); Scale stays 1 here.
    /// </summary>
    public class AttributeAffix : Affix
    {
        public string AttributeId;
        public float BaseValue;
        public float ValuePerLevel;
        [NonSerialized] public int AffixLevel;

        private StatModifier _addedModifier;

        [Serializable]
        private class AttributeAffixState
        {
            public int AffixLevel;
        }

        public override void SetAffixLevel(int level) => AffixLevel = level;

        // Only the mutable state (level) is saved; AttributeId/BaseValue/ValuePerLevel come from AffixData.
        public override string SerializeAffix()
        {
            return JsonUtility.ToJson(new AttributeAffixState { AffixLevel = AffixLevel });
        }

        public override void DeserializeAffix(string data)
        {
            if (string.IsNullOrEmpty(data)) return;
            var state = JsonUtility.FromJson<AttributeAffixState>(data);
            AffixLevel = state.AffixLevel;
        }

        public override void ApplyAffixToActor(Actor actor)
        {
            StatModifierData data = GetStatModifierData();
            StatsModule statsModule = actor.GetModule<StatsModule>();
            // Level = AffixLevel so the applied value matches what Stringfy() shows; affixes are not scaled
            // by item rarity (rarity governs how many affixes roll, not their magnitude), so Scale stays 1.
            _addedModifier = new StatModifier(data) { Level = AffixLevel };
            statsModule.AddModifier(_addedModifier);
        }

        public override void RemoveAffixFromActor(Actor actor)
        {
            if (_addedModifier == null) return;
            StatsModule statsModule = actor.GetModule<StatsModule>();
            statsModule.RemoveModifier(_addedModifier);
        }

        public StatModifierData GetStatModifierData()
        {
            StatModifierData statModifierData = new StatModifierData();
            AttributeAsset attributeAsset = RpgManager.GetAttributeAssetById(AttributeId);
            statModifierData.Stat = attributeAsset;
            statModifierData.BaseValue = BaseValue;
            statModifierData.LevelToValueFactor = ValuePerLevel;
            statModifierData.ModifierType = ModifierTypes.Flat;
            statModifierData.IsPercentage = false;
            return statModifierData;
        }

        public override string Stringfy()
        {
            AttributeAsset attr = RpgManager.GetAttributeAssetById(AttributeId);
            string attrName = attr != null ? attr.GetName() : AttributeId;
            StatModifierData statModifierData = GetStatModifierData();
            float value = statModifierData.GetValue(AffixLevel);
            return $"+{value} {attrName}";
        }
    }
}
