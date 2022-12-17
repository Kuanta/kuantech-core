using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Kuantech.Core
{
    public class Actor : MonoBehaviour
    {
        //public float stats
        public Rigidbody Rigidbody;
        public StatsModule Stats;
        public StatusEffectHandler StatusEffectHandler;
        public CombatModule CombatModule;
        public MovementModule MovementModule;

        public float Health; //Current value for health
        public float Energy; //Current value for energy

        public Vector3 ForceMoveVector = Vector3.zero;
        private Dictionary<Type, Module> _modules = new Dictionary<Type, Module>();
        
        //Events
        public EventHandler OnModulesInitialized;
        public EventHandler<float> OnDamageReceived;
        public EventHandler OnDeath;
        
        public virtual void Initialize()
        {
            Rigidbody = GetComponent<Rigidbody>();
            StatusEffectHandler = new StatusEffectHandler();
            Module[] modules = GetComponents<Module>();
            foreach (Module module in modules)
            {
                _modules[module.GetType()] = module;
                module.Initialize();
            }
            OnModulesInitialized?.Invoke(this, EventArgs.Empty);
            Reset();
        }

        public Module GetModuleByType(Type moduleType)
        {
            if (_modules.ContainsKey(moduleType)) return _modules[moduleType];
            return null;
        }
        
        protected virtual void Update()
        {
            //todo: Implement health and mana regeneration
            StatusEffectHandler?.Update();
            
            //Health and energy regen
            Health += Stats.GetStat(StatTypes.HealthRegeneration) * Time.deltaTime;
            Energy += Stats.GetStat(StatTypes.EnergyRegeneration) * Time.deltaTime;
            Health = Mathf.Clamp(Health, 0f, Stats.GetStat(StatTypes.MaxHealth));
            Energy = Mathf.Clamp(Energy, 0f, Stats.GetStat(StatTypes.MaxEnergy));
        }

        public virtual void SetHealth(float normalizedValue)
        {
            normalizedValue = Mathf.Clamp(normalizedValue, 0f, 1f);
            Health = Stats.GetStat(StatTypes.MaxHealth) * normalizedValue;
        }

        public virtual void ReceiveHeal(float heal)
        {
            Health += Mathf.Abs(heal);
            Health = Mathf.Clamp(Health, 0f, Stats.GetStat(StatTypes.MaxHealth));
        }

        public virtual void ReceivePercentageHeal(float percentage)
        {
            ReceiveHeal(Stats.GetStat(StatTypes.MaxHealth) * percentage);
        }
        
        public virtual void ReceiveEnergy(float energy)
        {
            Energy += Mathf.Abs(energy);
            Energy = Mathf.Clamp(Energy, 0f, Stats.GetStat(StatTypes.MaxEnergy));
        }

        public virtual void ReceivePercentageEnergy(float percentage)
        {
            ReceiveEnergy(Stats.GetStat(StatTypes.MaxEnergy) * percentage);    
        }
        
        public virtual void ReceiveDamage(Actor from, float damage)
        {
            Health -= Mathf.Abs(damage);
            if (Health <= 0f)
            {
                Death();
                return;
            }
            OnDamageReceived?.Invoke(this, damage);
        }

        public virtual void Death()
        {
            OnDeath?.Invoke(this, EventArgs.Empty);
        }

        public virtual void Reset()
        {
            Health = Stats.GetStat(StatTypes.MaxHealth);
            ForceMoveVector = Vector3.zero;
            foreach (var key in _modules.Keys)
            {
                _modules[key].Reset();
            }
        }

        [Button("Add Modifier Effect")]
        public void AddModifierStatusEffect(StatModifier modifier, float Duration)
        {
            ModifierStatusEffect modifierStatusEffect = new ModifierStatusEffect(modifier);
            modifierStatusEffect.Duration = Duration;
            modifierStatusEffect.TickPeriod = -1;
            modifierStatusEffect.Init(this);
            StatusEffectHandler.AddStatusEffect(modifierStatusEffect);
        }
    }
}