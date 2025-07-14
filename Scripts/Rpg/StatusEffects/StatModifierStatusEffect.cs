using System;
using Kuantech.Rpg;
using UnityEngine;

namespace Kuantech.Core.Combat
{
    [Serializable]
    public class StatModifierStatusEffectData : StatusEffectData
    {
        public StatModifierData StatModifierData;
    }
    
    [Serializable]
    public class StatModifierStatusEffect : StatusEffect
    {
        public StatModifier Modifier;

        public override void Initialize(StatusEffectAsset statusEffectAsset, StatusEffectData applyData)
        {
            base.Initialize(statusEffectAsset, applyData);
            if (applyData is StatModifierStatusEffectData modifierData)
            {
                Modifier = new StatModifier(modifierData.StatModifierData);
            }
            else
            {
                Debug.LogError($"Invalid StatusEffectData type. Expected ModifierStatusEffectData.");
            }    
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