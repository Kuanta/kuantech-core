using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.Rpg
{
    /// <summary>
    /// Defines an attribute definition but with string id instead of attribute asset
    /// </summary>
    [Serializable]
    public struct SerializableAttributeDefinition
    {
        public string AttributeId;
        public float BaseValue;
        public float ValuePerRak;
        public float ValuePerLevel;    
    }

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
            return BaseValue + Rank * ValuePerRank + level * ValuePerLevel;
        }

        public float GetValue(int level, StatsModule statsModule, HashSet<AttributeAsset> visiting = null)
        {
            float val = GetBaseValue(level);

            if (attributeAsset != null && attributeAsset.Dependencies != null)
            {
                visiting ??= new HashSet<AttributeAsset>();
                if (visiting.Add(attributeAsset))
                {
                    foreach (var dep in attributeAsset.Dependencies)
                    {
                        if (dep.DependentAttribute == null || dep.DependencyFormula == null) continue;
                        float source = statsModule.GetAttributeValue(dep.DependentAttribute, visiting);
                        val += dep.DependencyFormula.Evaluate(source);
                    }
                    visiting.Remove(attributeAsset);
                }
                else
                {
                    Debug.LogWarning($"[Attribute] Circular dependency on '{attributeAsset.Id}' — skipping.");
                }
            }

            val = (val + FlatModifier) * PercentAddModifier * PercentMultModifier;

            if (Limits.x != 0 || Limits.y != 0)
                val = Mathf.Clamp(val, Limits.x, Limits.y);

            return val;
        }
    }
}