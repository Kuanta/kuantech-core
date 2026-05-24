using System;
using Kuantech.Core;
using Kuantech.Core.Database.Attributes;
using Kuantech.Rpg.Managers;
using UnityEngine;

namespace Kuantech.Rpg
{
    public class AttributeAffix : Affix
    {
        [KtDatabaseVariable("AttributeId")] public string AttributeId { get; protected set; }
        [KtDatabaseVariable("BaseValue")] public float BaseValue { get; protected set; }
        [KtDatabaseVariable("ValuePerLevel")] public float ValuePerLevel { get; protected set; }
        [KtDatabaseVariable("RarityScales")] public float[] RarityScales { get; protected set; }
        [NonSerialized] public int AffixLevel;

        private StatModifier _addedModifier;

        [Serializable]
        private class AttributeAffixState
        {
            public string AffixId;
            public string AffixName;
            public float Weight;
            public string AttributeId;
            public float BaseValue;
            public float ValuePerLevel;
            public float[] RarityScales;
            public int AffixLevel;
        }

        public override string SerializeAffix()
        {
            return JsonUtility.ToJson(new AttributeAffixState
            {
                AffixId = AffixId,
                AffixName = AffixName,
                Weight = Weight,
                AttributeId = AttributeId,
                BaseValue = BaseValue,
                ValuePerLevel = ValuePerLevel,
                RarityScales = RarityScales,
                AffixLevel = AffixLevel
            });
        }

        public override void DeserializeAffix(string data)
        {
            var state = JsonUtility.FromJson<AttributeAffixState>(data);
            AffixId = state.AffixId;
            AffixName = state.AffixName;
            Weight = state.Weight;
            AttributeId = state.AttributeId;
            BaseValue = state.BaseValue;
            ValuePerLevel = state.ValuePerLevel;
            RarityScales = state.RarityScales;
            AffixLevel = state.AffixLevel;
        }

        public override void ApplyAffixToActor(Actor actor)
        {
            StatModifierData data = GetStatModifierData();
            StatsModule statsModule = actor.GetModule<StatsModule>();
            _addedModifier = new StatModifier(data);
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
