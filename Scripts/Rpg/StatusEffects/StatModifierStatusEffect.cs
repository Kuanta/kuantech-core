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
    
    public class StatModifierStatusEffect : StatusEffect
    {
        public StatModifier Modifier;

        public override void Init(StatusEffectData data)
        {
            base.Init(data);
            if (data is StatModifierStatusEffectData modifierData)
            {
                Modifier = new StatModifier(modifierData.StatModifierData);
            }
            else
            {
                Debug.LogError($"Invalid StatusEffectData type: {data.GetType().Name}. Expected ModifierStatusEffectData.");
            }    
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