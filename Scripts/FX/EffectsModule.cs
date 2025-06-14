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
        
        public List<EffectPlayer> EffectPlayers;
        private Dictionary<string, EffectPlayer> _effectsById;
        
        public override void Initialize()
        {
            base.Initialize();
            Actor.OnHitEvent += OnReceiveDamage;
            _effectsById = new Dictionary<string, EffectPlayer>();
            foreach(var effectPlayer in EffectPlayers)
            {
                if(!effectPlayer.EffectId.IsNullOrEmpty())
                {
                    _effectsById[effectPlayer.EffectId] = effectPlayer;
                }else if (effectPlayer.EffectPrefab != null && !effectPlayer.EffectPrefab.EffectId.IsNullOrEmpty())
                {
                    _effectsById[effectPlayer.EffectPrefab.EffectId] = effectPlayer;
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
    }
}