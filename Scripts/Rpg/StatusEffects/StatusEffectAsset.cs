using System;
using Kuantech.Core.FX;
using UnityEngine;

namespace Kuantech.Core.Combat
{

    public class StatusEffectAsset : MetadataAsset
    {
        public EffectPlayer EffectPlayer;
        public StatusEffectType StatusEffectType;
        
        [SerializeReference]
        public StatusEffectData StatusEffectData;
        
        public bool Stackable;
        [Tooltip("For non stackable status effects, the existing one will be refreshed")] 
        public bool RefreshOnApply;

        public StatusEffect CreateStatusEffect()
        {
            Type t = StatusEffectType.Type;
            StatusEffect statusEffect = (StatusEffect) Activator.CreateInstance(t);
            statusEffect.Initialize(this, StatusEffectData);
            return statusEffect;
        }
    }
}