using System.Collections.Generic;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Core.Combat
{
    public class StatusEffectHandler : ActorModule
    {
        public Queue<StatusEffect> EffectsToAdd = new Queue<StatusEffect>();
        public Queue<StatusEffect> EffectsToRemove = new Queue<StatusEffect>();

        private Dictionary<string, List<StatusEffect>> _statusEffectsMap =
            new Dictionary<string, List<StatusEffect>>();
        private List<StatusEffect> Effects = new List<StatusEffect>();
        
        /// <summary>
        /// Adds a status effect
        /// </summary>
        /// <param name="effect"></param>
        public void AddStatusEffect(StatusEffect effect)
        {
            EffectsToAdd.Enqueue(effect);
        }
        
        /// <summary>
        /// Removes a status effect
        /// </summary>
        /// <param name="effect"></param>
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
                    RemoveStatusEffect(effect);
                    continue;
                }

                if (effect.GetTickRate() > 0 && Time.time - effect.LastTickTime >= effect.GetTickRate())
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
                if (_AddEffect(effect))
                {
                    effect.OnAdd(Actor);
                }
                if (EffectsToAdd.IsNullOrEmpty()) break;
                effect = EffectsToAdd.Dequeue();
            }
        }

        private bool _AddEffect(StatusEffect effect)
        {
            if (_statusEffectsMap == null) _statusEffectsMap = new Dictionary<string, List<StatusEffect>>();
            string effectId = effect.GetId();
            bool stackable = effect.StatusEffectAsset.Stackable;
            
            //Is status effect exists
            if (!_statusEffectsMap.ContainsKey(effectId))
            {
                _statusEffectsMap[effectId] = new List<StatusEffect>();
            }

            if (_statusEffectsMap[effectId].Count == 0 || stackable)
            {
                _statusEffectsMap[effectId].Add(effect);
                Effects.Add(effect);
                return true;
            }
            
            //there are already status effects of the same type and its not stackable
            if (effect.StatusEffectAsset.RefreshOnApply)
            {
                //Don't add but refresh the existing one
                _statusEffectsMap[effectId][0].Refresh();
            }

            return false;
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
                _RemoveEffect(effect);
                if (EffectsToAdd.IsNullOrEmpty()) break;
                effect = EffectsToRemove.Dequeue();
            }
        }

        private void _RemoveEffect(StatusEffect effect)
        {
            Effects.Remove(effect);
            if (_statusEffectsMap.ContainsKey(effect.GetId()))
            {
                _statusEffectsMap[effect.GetId()].Remove(effect);
            }
            effect.OnRemove();
        }

        public void ClearStatusEffects()
        {
            foreach(var effect in Effects)
            {
                effect.OnRemove();
            }
        }

        public override void Cleanup()
        {
            base.Cleanup();
            ClearStatusEffects();
        }

        public override void ResetModule()
        {
            base.ResetModule();
            ClearStatusEffects();
        }
    }
}