using System;
using System.Collections.Generic;
using Kuantech.Rpg.Managers;
using Kuantech.Utils;
using UnityEngine;
#if NETWORKING_FISHNET
using FishNet.Object;
#endif

namespace Kuantech.Core.Combat
{
    [Serializable]
    public class StatusEffectHandlerState : ActorModuleSerializableData
    {
        public List<StatusEffectSerializableData> EffectStates;
    }

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
            if(IsServerInitialized)
            {
                ExecuteAddStatusEffect(effect);
                ObserversOnAddEffect_Rpc(effect.GetId(), effect.ApplyData.Duration, effect.ApplyData.TickPeriod, effect.Rank);
            }
        }

        private void ExecuteAddStatusEffect(StatusEffect effect)
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
            if (IsServerInitialized)
                ObserversOnRemoveEffect_Rpc(effect.GetId());
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
                if (EffectsToRemove.IsNullOrEmpty()) break;
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

        /// <summary>
        /// Ends every active effect and forgets them. OnRemove stops each effect's attached FX, so nothing
        /// keeps burning/glowing on a body that is no longer in that state.
        /// </summary>
        public void ClearStatusEffects()
        {
            foreach(var effect in Effects)
            {
                effect.OnRemove();
            }
            // Actually forget them — otherwise Update keeps ticking these effects on a reset actor.
            Effects.Clear();
            _statusEffectsMap?.Clear();
            EffectsToAdd.Clear();
            EffectsToRemove.Clear();
        }

        public override void OnActorStateChanged(ActorState oldState, ActorState newState)
        {
            base.OnActorStateChanged(oldState, newState);
            // Death ends every status effect: a corpse should not keep taking damage-over-time, and its
            // attached FX must stop here rather than lingering until despawn (a yeeted body would fly
            // across the arena still on fire).
            if (newState == ActorState.Dead) ClearStatusEffects();
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

        #region State

        protected override ActorModuleSerializableData InstantiateState()
        {
            var effectStates = new List<StatusEffectSerializableData>();
            foreach (var effect in Effects)
            {
                if (!effect.ToBeRemoved)
                    effectStates.Add(effect.BuildState());
            }
            return new StatusEffectHandlerState { EffectStates = effectStates };
        }

        public override void LoadState(ActorModuleSerializableData serializableData)
        {
            if (serializableData is not StatusEffectHandlerState state) return;
            if (state.EffectStates == null) return;
            foreach (var effectData in state.EffectStates)
            {
                if (effectData.ToBeRemoved) continue;
                StatusEffectAsset asset = RpgManager.GetStatusEffectAssetById(effectData.StatusEffectId);
                if (asset == null)
                {
                    continue;
                }
                StatusEffect effect = asset.CreateStatusEffect();
                effect.OnAdd(Actor);       // sets ApplyTime, spawns FX
                effect.ApplyState(effectData); // overrides timing with saved values
                // Skip queue — add directly so timing is correct immediately
                _AddEffect(effect);
            }
        }

        #endregion

#if NETWORKING_FISHNET
        [ObserversRpc]
        private void ObserversOnAddEffect_Rpc(string effectId, float duration, float tickPeriod, int rank)
        {
            if (IsServerInitialized) return;
            StatusEffectAsset asset = RpgManager.GetStatusEffectAssetById(effectId);
            if (asset == null) return;
            StatusEffect effect = asset.CreateStatusEffect();
            effect.SetRank(rank);
            var applyData = new StatusEffectApplyData { Duration = duration, TickPeriod = tickPeriod };
            effect.Initialize(asset, applyData);
            ExecuteAddStatusEffect(effect);
        }

        [ObserversRpc]
        private void ObserversOnRemoveEffect_Rpc(string effectId)
        {
            if (IsServerInitialized) return;
            if (!_statusEffectsMap.ContainsKey(effectId)) return;
            var list = _statusEffectsMap[effectId];
            if (list.Count == 0) return;
            RemoveStatusEffect(list[0]);
        }
#else
        private void ObserversOnAddEffect_Rpc(string effectId, float duration, float tickPeriod, int rank) { }
        private void ObserversOnRemoveEffect_Rpc(string effectId) { }
#endif
    }
}