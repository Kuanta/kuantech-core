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
        // Scratch list for ClearStatusEffects, reused so ending effects costs no allocation. Death clears
        // effects, and in a horde fight that runs many times a second.
        private readonly List<StatusEffect> _clearBuffer = new List<StatusEffect>();
        
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
            if (effect == null) return;
            // Already on its way out — queueing it twice would run OnRemove twice (stopping FX that a newly
            // applied effect of the same kind had just started).
            if (effect.ToBeRemoved) return;
            // Not ours, or already gone. This is a normal race rather than a fault: an effect can expire on
            // the same frame its target dies, and death clears the list first. It used to log an error here,
            // which on mobile meant capturing a stack trace every frame a burn finished off an enemy.
            if (!Effects.Contains(effect)) return;

            effect.ToBeRemoved = true;
            EffectsToRemove.Enqueue(effect);
            if (IsServerInitialized)
                ObserversOnRemoveEffect_Rpc(effect.GetId());
        }

        public override void ModuleUpdate(float deltaTime)
        {
            AddQueuedEffects();

            // Indexed, not foreach: a tick can end up mutating this list from underneath us. A damage-over-
            // time tick that kills its target puts the actor into Dead, and the death handler clears every
            // effect — so by the time OnTick returns, the collection we are walking may be empty. Re-reading
            // Count each step turns that into "stop ticking", which is exactly the right answer for a corpse,
            // instead of an InvalidOperationException thrown every frame something burns to death.
            for (int i = 0; i < Effects.Count; i++)
            {
                StatusEffect effect = Effects[i];
                if (effect == null || effect.ToBeRemoved) continue;

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
            // Drains whatever is queued, including anything OnAdd queues in turn (an effect that applies a
            // second one), so a chained application lands this frame rather than the next.
            while (EffectsToAdd.Count > 0)
            {
                StatusEffect effect = EffectsToAdd.Dequeue();
                if (effect == null) continue;
                if (_AddEffect(effect)) effect.OnAdd(Actor);
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
            while (EffectsToRemove.Count > 0)
            {
                StatusEffect effect = EffectsToRemove.Dequeue();
                if (effect == null) continue;
                _RemoveEffect(effect);
            }
        }

        private void _RemoveEffect(StatusEffect effect)
        {
            // Only end an effect this handler was actually still tracking. A clear (death, reset) ends every
            // effect itself, so anything left over in the queue must not have OnRemove run on it a second
            // time — that would stop FX belonging to a freshly applied effect of the same kind.
            bool wasTracked = Effects.Remove(effect);
            if (_statusEffectsMap.ContainsKey(effect.GetId()))
            {
                _statusEffectsMap[effect.GetId()].Remove(effect);
            }
            if (wasTracked) effect.OnRemove();
        }

        /// <summary>
        /// Ends every active effect and forgets them. OnRemove stops each effect's attached FX, so nothing
        /// keeps burning/glowing on a body that is no longer in that state.
        /// </summary>
        public void ClearStatusEffects()
        {
            // Empty the live collections BEFORE ending anything. OnRemove runs arbitrary effect code (stopping
            // FX, and in principle touching this handler again), and the common caller is death — reached from
            // inside the tick loop. Clearing first means whatever OnRemove does, it sees a handler that already
            // holds nothing, so nothing can be ticked, removed or reported twice.
            _clearBuffer.Clear();
            _clearBuffer.AddRange(Effects);

            // Actually forget them — otherwise Update keeps ticking these effects on a reset actor.
            Effects.Clear();
            _statusEffectsMap?.Clear();
            EffectsToAdd.Clear();
            EffectsToRemove.Clear();

            for (int i = 0; i < _clearBuffer.Count; i++)
            {
                StatusEffect effect = _clearBuffer[i];
                if (effect == null) continue;
                effect.ToBeRemoved = true;
                effect.OnRemove();
            }
            _clearBuffer.Clear();
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