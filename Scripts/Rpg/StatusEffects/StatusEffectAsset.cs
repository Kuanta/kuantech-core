using System;
using Kuantech.Core.FX;
using UnityEngine;

namespace Kuantech.Core.Combat
{
    [CreateAssetMenu(fileName = "Status Effect Assset", menuName = "Kuantech/Rpg/Status Effect Asset")]
    public class StatusEffectAsset : MetadataAsset
    {
        public EffectPlayer EffectPlayer;
        public StatusEffectType StatusEffectType;
        
        [SerializeReference]
        public StatusEffectData DefaultStatusEffectData;
        
        public bool Stackable;
        [Tooltip("For non stackable status effects, the existing one will be refreshed")] 
        public bool RefreshOnApply;

        public StatusEffect CreateStatusEffect(StatusEffectData applyData = null)
        {
            Type t = StatusEffectType.Type;
            StatusEffect statusEffect = (StatusEffect) Activator.CreateInstance(t);
            StatusEffectData dataToApply = applyData ?? DefaultStatusEffectData;
            statusEffect.Initialize(this, dataToApply);
            return statusEffect;
        }
        
        public T CreateStatusEffect<T>(StatusEffectData applyData = null) where T : StatusEffect, new()
        {
            T statusEffect = new T();
            StatusEffectData dataToApply = applyData ?? DefaultStatusEffectData;
            statusEffect.Initialize(this, dataToApply);
            return statusEffect;
        }
    }
}