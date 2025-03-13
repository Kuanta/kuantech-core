using System;
using System.Collections.Generic;
using Kuantech.Core.Combat;
using Kuantech.Rpg;
using Sirenix.Utilities;
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
        
        //[SerializeField] private Dictionary<int, >
        public override void Initialize()
        {
            base.Initialize();
            Actor.OnHitEvent += OnReceiveDamage;
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