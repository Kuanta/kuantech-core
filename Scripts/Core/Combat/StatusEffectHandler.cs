using System.Collections.Generic;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Core.Combat
{
    public class StatusEffectHandler
    {
        public Queue<StatusEffect> EffectsToAdd = new Queue<StatusEffect>();
        public Queue<StatusEffect> EffectsToRemove = new Queue<StatusEffect>();
        [SerializeField] private List<StatusEffect> Effects = new List<StatusEffect>();

        public void AddStatusEffect(StatusEffect effect)
        {
            EffectsToAdd.Enqueue(effect);
        }

        public void RemoveStatusEffect(StatusEffect effect)
        {
            if (!Effects.Contains(effect))
            {
                Debug.LogError("Effect not in list");
            }
            effect.ToBeRemoved = true;
            EffectsToRemove.Enqueue(effect);
        }

        public void Update()
        {
            AddQueuedEffects();
            foreach (var effect in Effects)
            {
                if(effect.ToBeRemoved) continue;
                if (effect.IsExpired())
                {
                    EffectsToRemove.Enqueue(effect);
                    continue;
                }

                if (effect.TickPeriod > 0 && Time.time - effect.LastTickTime >= effect.TickPeriod)
                {
                    effect.OnTick();
                    effect.LastTickTime = Time.time;
                }
            }
            RemoveQueuedEffects();
        }
        
        /// <summary>
        /// Adds the queued effects to the status list
        /// </summary>
        private void AddQueuedEffects()
        {
            if(EffectsToAdd.IsNullOrEmpty()) return;
            StatusEffect effect = EffectsToAdd.Dequeue();
            while (effect != null)
            {
                Effects.Add(effect);
                effect.OnAdd();
                if (EffectsToAdd.IsNullOrEmpty()) break;
                effect = EffectsToAdd.Dequeue();
            }
        }
        
        /// <summary>
        /// Remove queued effects from the status list
        /// </summary>
        private void RemoveQueuedEffects()
        {
            if(EffectsToRemove.IsNullOrEmpty()) return;
            StatusEffect effect = EffectsToRemove.Dequeue();
            while (effect != null)
            {
                Effects.Remove(effect);
                effect.OnRemove();
                if (EffectsToAdd.IsNullOrEmpty()) break;
                effect = EffectsToRemove.Dequeue();
            }
        }
    }
}