using System;
using Kuantech.Core;
using UnityEngine;
using Kuantech.Rpg;



[Serializable]
public class CombatVariable
{
    public float BaseValue = 0;
    public AttributeAsset AttributeAsset = null;
    public float AttributeScalar = 1;

    public virtual float GetValue(StatsModule statsModule = null)
    {
        if (statsModule == null || AttributeAsset == null) return BaseValue;
        return BaseValue + AttributeScalar * statsModule.GetAttributeValue(AttributeAsset);
    }
}

/// <summary>
/// Combat damage variable that handles damage type, attribute scaling, and critical hits.
/// </summary>
[Serializable]
public class CombatDamageVariable : CombatVariable
{
    public DamageType DamageType;
    public CombatVariable CriticalMultiplier;
    public float CriticalChance;

    public DamageInfo GetDamageInfo(StatsModule statsModule)
    {
        float baseValue = base.GetValue(statsModule);
        float critMultiplier = CriticalMultiplier != null ? CriticalMultiplier.GetValue(statsModule) : 1f;
        bool isCritical = critMultiplier > 1f && UnityEngine.Random.Range(0f, 1f) < CriticalChance;

        return new DamageInfo
        {
            DamageAmount = baseValue * Mathf.Max(1,critMultiplier),
            DamageType = DamageType,
            IsCritical = isCritical,
        };
    }
}
