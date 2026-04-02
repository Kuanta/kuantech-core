using System;
using System.Collections.Generic;
using Kuantech.Core.FX;
using Kuantech.Rpg.Skills;
using UnityEngine;

namespace Kuantech.Core.Combat
{
    [Serializable]
    public abstract class StatusEffectConfig
    {
        
    }
    
    [CreateAssetMenu(fileName = "Status Effect Assset", menuName = "Kuantech/Rpg/Status Effect Asset")]
    public class StatusEffectAsset : MetadataAsset
    {
        public EffectPlayer EffectPlayer;
        public StatusEffectType StatusEffectType;

        [Header("Config")]
        [SerializeReference] public StatusEffectConfig Config;
        
        [Header("Variables")] 
        public List<StatusEffectVariableData> StatusEffectVariables;
        
        [SerializeReference]
        public StatusEffectApplyData DefaultStatusEffectApplyData;
        
        public bool Stackable;
        [Tooltip("For non stackable status effects, the existing one will be refreshed")] 
        public bool RefreshOnApply;

        public StatusEffect CreateStatusEffect(StatusEffectApplyData applyApplyData = null)
        {
            Type t = StatusEffectType.Type;
            StatusEffect statusEffect = (StatusEffect) Activator.CreateInstance(t);
            StatusEffectApplyData applyDataToApply = applyApplyData ?? DefaultStatusEffectApplyData;
            statusEffect.Initialize(this, applyDataToApply);
            return statusEffect;
        }
        
        public T CreateStatusEffect<T>(StatusEffectApplyData applyApplyData = null) where T : StatusEffect, new()
        {
            T statusEffect = new T();
            StatusEffectApplyData applyDataToApply = applyApplyData ?? DefaultStatusEffectApplyData;
            statusEffect.Initialize(this, applyDataToApply);
            return statusEffect;
        }
        
        public T GetConfig<T>() where T : StatusEffectConfig
        {
            return (T) Config;
        }
    }
}