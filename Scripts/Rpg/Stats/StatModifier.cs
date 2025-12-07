using System;
using UnityEngine.Serialization;

namespace Kuantech.Rpg
{
    public enum ModifierTypes
    {
        Flat,
        PercentAdd,
        PercentMult,
    }
    
    [Serializable]
    public struct StatModifierData
    {
        public AttributeAsset Stat;
        public string ModifierTag;
        public float BaseValue;
        public float LevelToValueFactor;
        public ModifierTypes ModifierType;
        public bool IsPercentage;
        public float GetValue(int level)
        {
            return BaseValue + LevelToValueFactor * level * (BaseValue != 0 ? Math.Sign(BaseValue) : 1);
        }
    }

    [Serializable]
    public class StatModifier
    {
        public int Level = 0; //Required for items
        public string ModifierTag = "";
        public AttributeAsset AttributeAsset;
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