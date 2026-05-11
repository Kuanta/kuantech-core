using System;
using Kuantech.Core;
using UnityEngine;
using Kuantech.Utils;

namespace Kuantech.Rpg
{
    [Serializable]
    ///Defines a variable that scales with a stat value
    public class AttributeBasedVariable
    {
        public AttributeAsset AttributeAsset = null;
        [SerializeReference] public KtFormula ScaleFormula = new KtLinearFormula();
        public virtual float GetValue(StatsModule statsModule = null)
        {
            float attributeValue = (AttributeAsset != null && statsModule != null) ? statsModule.GetAttributeValue(AttributeAsset) : 0;
            if (ScaleFormula == null) return attributeValue;
            return ScaleFormula.Evaluate(attributeValue);
        }
    }


    /// <summary>
    /// Combat damage variable that handles damage type, attribute scaling, and critical hits.
    /// </summary>
    [Serializable]
    public class AtributeBasedDamageVariable
    {
        public DamageType DamageType;
        [SerializeReference] public KtFormula ScaleFormula = new KtLinearFormula();
        public AttributeBasedVariable CriticalMultiplier;
        public AttributeBasedVariable CriticalChance;

        public DamageInfo GetDamageInfo(StatsModule statsModule)
        {
            //Calculate damage
            AttributeAsset attAsset = DamageType.DamageScaleAttribute;
            float attributeValue = (attAsset != null && statsModule != null) ? statsModule.GetAttributeValue(attAsset) : 0;
            float damage = 0f;
            if (ScaleFormula != null)
            {
                damage = ScaleFormula.Evaluate(attributeValue);
            }

            float criticalChance = Mathf.Clamp01((CriticalChance != null) ? CriticalChance.GetValue(statsModule) : 0);
            bool cr = UnityEngine.Random.Range(0f, 1f) < criticalChance;

            float criticalMultiplier = (CriticalMultiplier != null) ? Mathf.Max(1, CriticalMultiplier.GetValue(statsModule)) : 1f;

            damage *= (cr ? criticalMultiplier : 1);

            return new DamageInfo
            {
                DamageAmount = damage,
                DamageType = DamageType,
                IsCritical = cr,
            };
        }
    }

}
