using System;
using System.Collections.Generic;
using Kuantech.Utils;
using UnityEngine;
using UnityEngine.Serialization;

namespace Kuantech.Core.FX
{
    /// <summary>
    /// This module handles the effects that are attached to the character
    /// </summary>
    public class EffectsModule : ActorModule
    {
        [Header("Pre-defined Effects")]
        public Effect DamageReceiveEffect;
        public Effect JumpEffect;
        public Effect DodgeEffect;
        public Effect DeathEffect;
        private Effect _impact;

        [Header("Existing Effects")]
        public List<Effect> ExistingEffects;
        private Dictionary<string, Effect> _effectsById;
        
        [Header("Existing Effect Players")]
        public List<EffectPlayerComponent> ExistingEffectPlayerComponents;
        private Dictionary<int, EffectPlayerComponent> _effectPlayerComponentsByTag;

        [Header("Shader Effects")]
        public List<ShaderEffect> ExistingShaderEffects;
        public HashSet<ShaderEffect> ShaderEffects = new HashSet<ShaderEffect>();
        private Dictionary<string, ShaderEffect> _shaderEffectsById = new Dictionary<string, ShaderEffect>();
        [NonSerialized] public HashSet<Effect> ActiveEffects = new HashSet<Effect>();

        public override void Initialize()
        {
            base.Initialize();
            Actor.OnHitEvent += OnReceiveDamage;
            _effectsById = new Dictionary<string, Effect>();
            foreach(var effect in ExistingEffects)
            {
                string effectId = effect.EffectId;
                if (!effectId.IsNullOrEmpty())
                {
                    _effectsById.Add(effectId, effect);
                }
                else
                {
                    Debug.LogWarning(
                        "Effect in effects module has no EffectId or EffectPrefab set. This effect will not be playable." +
                        " Please set an EffectId or EffectPrefab to the effect player.");
                }
            }
            
            SetEffectPlayers();
            
            //Set shader effects
            foreach (var shaderEffect in ExistingShaderEffects)
            {
                AddShaderEffect(shaderEffect);
            }
        }

        private void SetEffectPlayers()
        {
            _effectPlayerComponentsByTag = new Dictionary<int, EffectPlayerComponent>();
            foreach (var effectPlayerComponent in ExistingEffectPlayerComponents)
            {
                _effectPlayerComponentsByTag[effectPlayerComponent.EffectPlayer.EffectTag] = effectPlayerComponent;
            }
        }
        public override void OnModulesInitialized()
        {
            base.OnModulesInitialized();
            ActorVisual actorVisual = Actor.VisualHandler.GetActorVisual();
            if (actorVisual != null)
            {
                UpdateShaderEffectRenderers(actorVisual.gameObject);
            }
            else
            {
                UpdateShaderEffectRenderers(gameObject);
            }

            Actor.VisualHandler.OnActorVisualSet += OnActorVisualSet;
        }

        public void OnActorVisualSet(ActorVisual actorVisual)
        {
            UpdateShaderEffectRenderers(actorVisual.gameObject);
            ActorVisualEffectsModule actorVisualEffectsModule = actorVisual.GetModule<ActorVisualEffectsModule>();
            if (actorVisualEffectsModule == null) return;
            foreach (var effectComp in actorVisualEffectsModule.EffectPlayersComponents)
            {
                _effectPlayerComponentsByTag[effectComp.EffectPlayer.EffectTag] = effectComp;
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
        
        /// <summary>
        /// Returns an effect player by tag
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        public EffectPlayerComponent GetEffectPlayerByTag(int tag)
        {
            if(_effectPlayerComponentsByTag.TryGetValue(tag, out var value)) return value;
            return null;
        }

        public void PlayEffectByTag(int tag)
        {
            GetEffectPlayerByTag(tag)?.PlayEffect(Actor);
        }
        #endregion

        #region Shader Effects

        public ShaderEffect GetShaderEffect(string effectId)
        {
            if (_shaderEffectsById.ContainsKey(effectId)) return _shaderEffectsById[effectId];
            return null;
        }
        
        public void PlayShaderEffect(string shaderEffect)
        {
            ShaderEffect effect = GetShaderEffect(shaderEffect);
            if (effect == null) return;
            effect.PlayShaderEffect();
        }

        public void StopShaderEffect()
        {
                
        }
        
        /// <summary>
        /// Adds a shader effect
        /// </summary>
        /// <param name="shaderEffect"></param>
        public void AddShaderEffect(ShaderEffect shaderEffect)
        {
            if (!string.IsNullOrEmpty(shaderEffect.EffectId))
            {
                _shaderEffectsById.Add(shaderEffect.EffectId, shaderEffect);
            }
            shaderEffect.transform.SetParent(transform);
            ShaderEffects.Add(shaderEffect);
        }

        public void RemoveShaderEffect(ShaderEffect shaderEffect)
        {
            if (!string.IsNullOrEmpty(shaderEffect.EffectId) && _effectsById.ContainsKey(shaderEffect.EffectId))
            {
                _effectsById.Remove(shaderEffect.EffectId);
            }

            ShaderEffects.Remove(shaderEffect);
        }

        public void UpdateShaderEffectRenderers(GameObject renderersParent)
        {
            foreach (var shaderEffect in ShaderEffects)
            {
                shaderEffect.DetectAllRenderers(renderersParent);
            }
        }

        #endregion

        #region Runtime Attached Effects
        
        /// <summary>
        /// Plays an effect on the actor
        /// </summary>
        /// <param name="effectPlayer"></param>
        public Effect PlayEffectOnActor(EffectPlayer effectPlayer, Vector3 localPos, Quaternion effectRotation)
        {
            EffectPlaySettings playSettings = EffectPlaySettings.GetPlayAtObjectSettings(Actor.transform, localPos, effectRotation);
            
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

        public void StopActiveEffects()
        {
            foreach (var activeFx in ActiveEffects)
            {
                activeFx.Stop();
            }
            ActiveEffects.Clear();
        }
        #endregion
    }
}