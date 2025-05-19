using System;
using UnityEngine.Serialization;

namespace Kuantech.Core
{
    public enum ModifierTypes
    {
        Addition,
        Multiplication,
    }
    
    [Serializable]
    public struct StatModifierData
    {
        public StatAttributeAsset Stat;
        public string ModifierTag;
        public float BaseValue;
        public float LevelToValueFactor;
        public ModifierTypes ModifierType;
        public bool IsPercentage;

        public float GetValue(int level)
        {
            return BaseValue + LevelToValueFactor * level * Math.Sign(BaseValue);
        }
    }

    [Serializable]
    public class StatModifier
    {
        public int Level = 0; //Required for items
        public string ModifierTag = "";
        public StatAttributeAsset AttributeAsset;
        public float BaseValue;
        public float LevelToValueFactor = 1;
        private StatModifierData _data;
        public StatModifier() { }
        public StatModifier(StatModifierData data)
        {
            _data = data;
            BaseValue = data.BaseValue;
            ModifierTag = data.ModifierTag;
            LevelToValueFactor = data.LevelToValueFactor;
            ModifierType = data.ModifierType;
            AttributeAsset = data.Stat;
        }
        public float GetValue()
        {
            return BaseValue + LevelToValueFactor * Level;
        }
        public ModifierTypes ModifierType;
    }
}