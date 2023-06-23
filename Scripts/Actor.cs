using System;
using System.Collections.Generic;
using Kuantech.Character;
using Kuantech.Core.HyperCasual;
using Kuantech.Inventory;
using Kuantech.Inventory.Items;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Kuantech.Core
{
    public class Actor : MonoBehaviour, ISpawnable
    {
        [Header("Properties")] 
        public uint FactionId;
        
        [Header("Visuals")] 
        public GameObject VisualModel;

        //public float stats
        [Header("Components")] 
        public Rigidbody Rigidbody;
        public Collider Collider;
        public StatsModule Stats;
        public StatusEffectHandler StatusEffectHandler;
        public CombatModule CombatModule;
        public MovementModule MovementModule;
        public AnimatorModule AnimatorModule;
        public InventoryModule InventoryModule;
        public CharacterBody CharacterBody;
        
        private float _normalizedHealth = 1f;
        public float Health
        {
            get => Stats.GetStat(StatTypes.MaxHealth) * _normalizedHealth;
            private set => _normalizedHealth = Mathf.Clamp(value / Stats.GetStat(StatTypes.MaxHealth), 0f, 1f);
        }
        
        private float _normalizedEnergy = 1f;
        public float Energy
        {
            get => Stats.GetStat(StatTypes.MaxEnergy) * _normalizedEnergy;
            private set => _normalizedEnergy = Mathf.Clamp(value / Stats.GetStat(StatTypes.MaxEnergy), 0f, 1f);
        }


        //public Vector3 ForceMoveVector = Vector3.zero;
        private Dictionary<Type, Module> _modules = new Dictionary<Type, Module>();


        //Events
        public EventHandler OnModulesInitialized;
        public EventHandler<float> OnDamageReceived;
        public EventHandler OnDeath;
        public EventHandler OnRespawnEvent;

        private bool _initialized = false;
        public virtual void Initialize()
        {
            if (_initialized) return;
            Rigidbody = GetComponent<Rigidbody>();
            StatusEffectHandler = new StatusEffectHandler();
            Module[] modules = GetComponents<Module>();
            foreach (Module module in modules)
            {
                _modules[module.GetType()] = module;
                OnDeath += module.OnDeath;
                OnRespawnEvent += module.OnRespawn;
                module.Actor = this;
                module.Initialize();
            }
            //Quick references
            Stats = (StatsModule) GetModuleByType(typeof(StatsModule));
            AnimatorModule = (AnimatorModule) GetModuleByType(typeof(AnimatorModule));
            MovementModule = (MovementModule) GetModuleByType(typeof(MovementModule));
            CombatModule = (CombatModule) GetModuleByType(typeof(CombatModule));
            InventoryModule = (InventoryModule) GetModuleByType(typeof(InventoryModule));
            OnModulesInitialized?.Invoke(this, EventArgs.Empty);
            Reset();
            _initialized = true;
        }

        public Module GetModuleByType(Type moduleType)
        {
            if (_modules.ContainsKey(moduleType)) return _modules[moduleType];
            return null;
        }
        
        protected virtual void Update()
        {
            if (Stats == null) return;
            if (GameManager.Instance.GameIsPaused || Health <= 0f) return;
            //todo: Implement health and mana regeneration
            StatusEffectHandler?.Update();
            
            //Health and energy regen
            ReceiveHeal(Stats.GetStat(StatTypes.HealthRegeneration) * Time.deltaTime);
            ReceiveEnergy(Stats.GetStat(StatTypes.EnergyRegeneration) * Time.deltaTime);
        }

        public virtual void SetHealth(float normalizedValue)
        {
            normalizedValue = Mathf.Clamp(normalizedValue, 0f, 1f);
            _normalizedHealth = normalizedValue;
            Health = Stats.GetStat(StatTypes.MaxHealth) * normalizedValue;
        }

        public virtual void SetEnergy(float normalizedValue)
        {
            normalizedValue = Mathf.Clamp(normalizedValue, 0f, 1f);
            _normalizedEnergy = normalizedValue;
        }
        public virtual void ReceiveHeal(float heal)
        {
            Health += Mathf.Abs(heal);
        }
        
        public virtual void ReceivePercentageHeal(float percentage)
        {
            ReceiveHeal(Stats.GetStat(StatTypes.MaxHealth) * percentage);
        }
        
        public virtual void ReceiveEnergy(float energy)
        {
            Energy += Mathf.Abs(energy);
        }

        public virtual void ReceivePercentageEnergy(float percentage)
        {
            ReceiveEnergy(Stats.GetStat(StatTypes.MaxEnergy) * percentage);    
        }
        
        public virtual float ReceiveDamage(Actor from, float damage, bool rawDamage = false)
        {
            if (Health <= 0 || damage <= 0) return 0; //Don't hit the dead no more
            
            //If not raw damage, apply armor reduction
            if (!rawDamage)
            {
                float armorValue = Stats.GetStat(StatTypes.Armor);
                damage *= (1 - Mathf.Exp(-damage/(armorValue*Config.ARMOR_COEFF)));
            }

            Health -= Mathf.Abs(damage);
            if (Health <= 0f)
            {
                Health = 0f;
                Death();
                return damage;
            }
            OnDamageReceived?.Invoke(this, damage);
            return damage;
        }

        public void RemoveHealth(float healthToRemove)
        {
            Health -= healthToRemove;
        }

        /// <summary>
        /// Removes energy from the actor
        /// </summary>
        /// <param name="energyToSpend"></param>
        /// <returns></returns>
        public virtual float SpendEnergy(float energyToSpend)
        {
            Energy -= Mathf.Abs(energyToSpend);
            Energy = Mathf.Max(Energy, 0f);
            return Energy;
        }
        /// <summary>
        /// Instantly kills the actor
        /// </summary>
        public void Kill()
        {
            _normalizedHealth = 0f;
            Death();
        }
        
        public virtual void Death()
        {
            OnDeath?.Invoke(this, EventArgs.Empty);
            if(Collider != null) Collider.enabled = false;
        }

        public virtual void Respawn()
        {
            OnRespawnEvent?.Invoke(this, EventArgs.Empty);
            Reset();
        }
        
        public virtual void Reset()
        {
            _normalizedHealth = 1f;
            _normalizedEnergy = 1f;
            if(Collider != null) Collider.enabled = true;
            foreach (var key in _modules.Keys)
            {
                _modules[key].Reset();
            }
            Stats.UpdateStatModifiers();
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

        public void ChangeCharacterBody(CharacterBody newBody)
        {
            if(CharacterBody != null) CharacterBody.gameObject.SetActive(false);
            List<Item> swappedItems = new List<Item>();
            if (InventoryModule != null && InventoryModule.equipment != null)
            {
                //Unequip items first
                foreach (var slot in InventoryModule.equipment.slotTable.Keys)
                {
                    if (InventoryModule.equipment.slotTable[slot].item == null) continue;
                    Item item = InventoryModule.equipment.slotTable[slot].item;
                    InventoryModule.UnequipItem(InventoryModule.equipment.slotTable[slot].item);
                    swappedItems.Add(item);
                }
            }
            CharacterBody = newBody;
            CharacterBody.SetActor(this);
            CharacterBody.ToggleAllDefaultInplaceEquipments(true);
            newBody.gameObject.SetActive(true);
            if (InventoryModule == null || InventoryModule.equipment == null) return;
            foreach (var item in swappedItems)
            {
                if(item == null) continue; //SHOULD NEVER BE THE CASE
                InventoryModule.EquipItem(item, item.slotType);
            }
        }


        public void OnSpawn()
        {
            Initialize();
        }

        public void OnRespawn()
        {
        }

        public void OnDespawn()
        {
            Destroy(gameObject); //todo: Use pool
        }
    }
    
    
}