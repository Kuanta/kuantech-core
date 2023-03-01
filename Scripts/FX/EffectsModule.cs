using System;
using System.Collections.Generic;
using Sirenix.Utilities;
using UnityEngine;

namespace Kuantech.Core.FX
{
    /// <summary>
    /// This module handles the effects that are attached to the character
    /// </summary>
    public class EffectsModule : Module
    {
        [SerializeField] private List<Effect> AttackEffects = new List<Effect>(); //Dependent on the weapon
        public Effect DamageReceiveEffect;
        public Effect DeathEffect;
        private Effect _impact;
        
        //[SerializeField] private Dictionary<int, >
        public override void Initialize()
        {
            base.Initialize();
            Actor.OnDamageReceived += OnReceiveDamage;
            Actor.OnDeath += OnDeath;
        }

        public override void OnModulesInitialized(object sender, EventArgs args)
        {
            base.OnModulesInitialized(sender, args);
            CombatModule cm = (CombatModule)Actor.GetModuleByType(typeof(CombatModule));
            cm.AttackStartEvent+= OnAttack;
            cm.MeleeImpactEvent += OnMeleeImpact;
        }

        public void SetAttackEffects(List<EffectTypes> effectTypes)
        {
            RemoveCurrentAttackEffects();
            
            foreach (var effectType in effectTypes)
            {
                Effect effect = EffectsLibrary.Instance.GetEffect(effectType);
                AttackEffects.Add(effect);
                effect.transform.SetParent(transform);
                effect.transform.localPosition = Vector3.zero;
                effect.transform.localRotation = Quaternion.identity;
            }
        }

        public void SetImpactEffect(Effect impactEffectPrefab)
        {
            RemoveImpactEffect();
            _impact = GameManager.Instance.Pool.GetObject(impactEffectPrefab.gameObject).GetComponent<Effect>();
        }
        
        public void RemoveCurrentAttackEffects()
        {
            //Clear existing ones
            foreach (var effect in AttackEffects)
            {
                EffectsLibrary.Instance.EffectsPool.PoolObject(effect.gameObject);
            }
            AttackEffects.Clear();
        }

        public void RemoveImpactEffect()
        {
            if (_impact != null)
            {
                GameManager.Instance.Pool.PoolObject(_impact.gameObject);
            }
        }
        private void OnAttack(object sender, int attackIndex)
        {
            if (AttackEffects.IsNullOrEmpty()) return;
            int effectIndex = attackIndex % AttackEffects.Count;
            AttackEffects[effectIndex].Play();
        }

        private void OnMeleeImpact(object sender, Actor target)
        {
            if (_impact == null) return;
            _impact.transform.position = target.transform.position;
            Vector3 diff = target.transform.position - transform.position;
            diff.y = 0;
            _impact.transform.rotation = Quaternion.LookRotation(diff);
            _impact.Play();
        }
        private void OnReceiveDamage(object sender, float damage)
        {
            if (DamageReceiveEffect != null)
            {
                DamageReceiveEffect.Play();
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