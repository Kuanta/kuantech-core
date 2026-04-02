using System;
using UnityEngine;

namespace Kuantech.Rpg
{
    /// <summary>
    /// Defines default values for an attribute
    /// </summary>
    [Serializable]
    public class AttributeDefinition
    {
        public AttributeAsset AttributeAsset;
        public float BaseValue;
        public float ValuePerRank;
        public float ValuePerLevel;

        public float GetValue(int level, int rank)
        {
            return BaseValue + ValuePerLevel * level + ValuePerRank * ValuePerRank;
        }
    }

    /// <summary>
    /// A Stat is a levelable variable. Damage, Range, AttackSpeed can be defined as stats.
    /// Stats are increased by the overall level as well as with their ranks. 
    /// </summary>
    [Serializable]
    public class Attribute
    {
        public AttributeAsset attributeAsset;

        [Tooltip("Value at Rank 0 and Level 0")]
        public float BaseValue;

        [Tooltip("Value gained every rank")] public float ValuePerRank;
        
        [Tooltip("Value gained every level")] public float ValuePerLevel;
        
        [Tooltip("Lower and upper boundaries for the attribute")]
        public Vector2 Limits;
        
        //Runtime
        public float FlatModifier;
        public float PercentAddModifier = 1f;
        public float PercentMultModifier = 1f;
        public int Rank;
        
        public void ApplyAttributeDefinition(AttributeDefinition definition)
        {
            attributeAsset = definition.AttributeAsset;
            BaseValue = definition.BaseValue;
            ValuePerRank = definition.ValuePerRank;
            ValuePerLevel = definition.ValuePerLevel;
        }
        
        public float GetBaseValue(int level)
        {
            float finalValue = BaseValue + Rank * ValuePerRank + level * ValuePerLevel;
            if (Limits.x != 0 && Limits.y != 0)
            {
                finalValue = Mathf.Clamp(finalValue, Limits.x, Limits.y);
            }

            return finalValue;
        }
        


        public float GetValue(int level)
        {
            float baseVal = GetBaseValue(level);
    
            float val = baseVal + FlatModifier;
    
            val *= PercentAddModifier;
    
            val *= PercentMultModifier;

            return val;
        }
    }
}