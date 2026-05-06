using System;
using Kuantech.Rpg;
using UnityEngine;

namespace Kuantech.Core.Combat
{
   
    
    [Serializable]
    public class StatModifierStatusEffectConfig : StatusEffectConfig
    {
        public StatModifierData StatModifierData;
    }
    
    [Serializable]
    public class StatModifierStatusEffect : StatusEffect
    {
        public StatModifier Modifier;

        public override void Initialize(StatusEffectAsset statusEffectAsset, StatusEffectApplyData applyApplyData)
        {
            base.Initialize(statusEffectAsset, applyApplyData);
            var config = (StatModifierStatusEffectConfig)statusEffectAsset.Config;
            Modifier = new StatModifier(config.StatModifierData);
        }

        public override void SetRank(int rank)
        {
            base.SetRank(rank);
            if (Modifier == null) return;
            Modifier.Level = rank;
        }
        
        public override void OnAdd(Actor targetActor)
        {
            base.OnAdd(targetActor);
            if (Modifier == null) return;
            Target.GetModule<StatsModule>().AddModifier(Modifier);
        }
        
        public override void OnRemove()
        {
            base.OnRemove();
            if (Modifier == null) return;
            StatsModule statModule = Target.GetModule<StatsModule>();
            statModule.RemoveModifier(Modifier);
        }
    }
}