using System;
using System.Collections.Generic;
using Kuantech.Core.Combat;
using Kuantech.Rpg;
using Kuantech.Utils;
using Sirenix.Utilities;
using Unity.VisualScripting;
using UnityEngine;

namespace Kuantech.Core.FX
{
    /// <summary>
    /// This module handles the effects that are attached to the character
    /// </summary>
    public class EffectsModule : ActorModule
    {
        public Effect DamageReceiveEffect;
        public Effect JumpEffect;
        public Effect DodgeEffect;
        public Effect DeathEffect;
        private Effect _impact;
        
        public List<Effect> ExistingEffects;
        private Dictionary<string, Effect> _effectsById;

        [NonSerialized] public HashSet<Effect> ActiveEffects = new HashSet<Effect>();

        public override void Initialize()
        {
            base.Initialize();
            Actor.OnHitEvent += OnReceiveDamage;
            _effectsById = new Dictionary<string, Effect>();
            foreach(var effectPlayer in ExistingEffects)
            {
                string effectId = effectPlayer.EffectId;
                if (!effectId.IsNullOrEmpty())
                {
                    _effectsById.Add(effectId, effectPlayer);
                }
                else
                {
                    Debug.LogWarning(
                        "Effect in effects module has no EffectId or EffectPrefab set. This effect will not be playable." +
                        " Please set an EffectId or EffectPrefab to the effect player.");
                }
            }
        }

        private void OnReceiveDamage(HitInfo hitInfo)
        {
            if (DamageReceiveEffect != null)
            {
                DamageReceiveEffect.Play();
            }
        }

        private void OnDodge(object sender, EventArgs args)
        {
            if (DodgeEffect != null)
            {
                DodgeEffect.Play();
            }
        }
        private void OnJump(object sender, EventArgs args)
        {
            if (JumpEffect != null)
            {
                JumpEffect.Play();
            }
        }
        
        private void OnDeath(object sender, EventArgs empty)
        {
            if (DeathEffect != null)
            {
                DeathEffect.Play();
            }
        }

        public override void Reset()
        {
            base.Reset();
            if(DeathEffect != null) DeathEffect.Stop();
            if(DamageReceiveEffect != null) DamageReceiveEffect.Stop();
            
        }
        
        #region Fx Players
        public Effect GetExistingEffect(string effectId)
        {
            if (effectId.IsNullOrEmpty()) return null;
            if (_effectsById.ContainsKey(effectId)) return _effectsById[effectId];
            return null;
        }
        #endregion

        #region Runtime Attached Effects
        
        /// <summary>
        /// Plays an effect on the actor
        /// </summary>
        /// <param name="effectPlayer"></param>
        public Effect PlayEffectOnActor(EffectPlayer effectPlayer)
        {
            EffectPlaySettings playSettings = EffectPlaySettings.GetPlayAtObjectSettings(Actor.transform, Vector3.zero, Quaternion.identity);
            
            //Does the effect is already on the actor?
            Effect existingEffect = GetExistingEffect(effectPlayer.GetEffectId());
            if (existingEffect != null)
            {
                return PlayExistignEffect(existingEffect.EffectId);
            }
            
            //Effect isnt in the existing effects
            Effect effect = effectPlayer.PlayEffect(playSettings);
            if (effect == null) return null;
            AddActiveEffect(effect);
            return effect;
        }

        /// <summary>
        /// Plays an existing effect
        /// </summary>
        /// <param name="effectId"></param>
        /// <returns></returns>
        public Effect PlayExistignEffect(string effectId)
        {
            Effect existingEffect = GetExistingEffect(effectId);
            if (existingEffect == null) return null;
            existingEffect.Play();
            return existingEffect;
        }
        
        /// <summary>
        /// Adds an active effect
        /// </summary>
        /// <param name="effect"></param>
        public void AddActiveEffect(Effect effect)
        {
            if (ActiveEffects == null) ActiveEffects = new HashSet<Effect>();
            ActiveEffects.Add(effect);
        }
        
        /// <summary>
        /// Stops an active effect
        /// </summary>
        /// <param name="effect"></param>
        public void StopActiveEffect(Effect effect)
        {
            if (ActiveEffects.IsNullOrEmpty() || !ActiveEffects.Contains(effect)) return;
            ActiveEffects.Remove(effect);
            effect.Stop();
        }
        #endregion
    }
}