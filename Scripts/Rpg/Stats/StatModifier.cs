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
        public float GetValue(int level, float scale=1f)
        {
            return StatModifier.GetStatModifierValue(BaseValue, LevelToValueFactor, level, scale);
        }
    }

    [Serializable]
    public class StatModifier
    {
        public int Level = 0; //Required for items
        public float Scale = 1;
        public string ModifierTag = "";
        public AttributeAsset AttributeAsset;
        public float BaseValue;
        public float LevelToValueFactor = 1;
        public StatModifier() { }
        public StatModifier(StatModifierData data)
        {
            BaseValue = data.BaseValue;
            ModifierTag = data.ModifierTag;
            LevelToValueFactor = data.LevelToValueFactor;
            ModifierType = data.ModifierType;
            AttributeAsset = data.Stat;
        }
        // Computes from this instance's own fields, so it is correct however the modifier was built — via the
        // StatModifierData constructor OR via object initializer (perks, network deserialize, ...).
        public float GetValue()
        {
            return GetStatModifierValue(BaseValue, LevelToValueFactor, Level, Scale);
        }

        public static float GetStatModifierValue(StatModifierData data, int modifierLevel, float scale=1)
        {
            return GetStatModifierValue(data.BaseValue, data.LevelToValueFactor, modifierLevel, scale);
        }

        public static float GetStatModifierValue(float baseValue, float levelToValueFactor, int modifierLevel, float scale=1)
        {
            return scale * (baseValue + levelToValueFactor * modifierLevel * (baseValue != 0 ? Math.Sign(baseValue) : 1));
        }
        public ModifierTypes ModifierType;
    }
}