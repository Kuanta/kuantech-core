using System.Collections;
using Kuantech.Core.FX;
using Kuantech.Rpg;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.Core.Combat
{
    /// <summary>
    /// Healthcare 
    /// </summary>
    public class HealthcareModule : ActorModule
    {
        [Header("Resources")]
        public ResourceAsset HealthResourceAsset;
        
        public bool DespawnAfterDeath = true;
        [Tooltip("Delay that despawns the actor after death")] public float DespawnDelay = 1f;

        [Header("Resistance")] 
        public AttributeAsset ArmorAttribute;
        public DamageReductionFormula DamageReductionFormula;
        
        [Header("UI")] 
        [SerializeField] private Healthbar Healthbar;
        [SerializeField] private bool ShowDamageText = false;

        [Header("Effects")]
        [SerializeField] private Effect HealEffect;
        
        //Events
        public UnityAction<HealthcareModule> OnHealthChanged;
        public UnityAction<DamageInfo> OnHealReceived;
        
        //Runtime 
        private StatsModule _statModule;
        public override void Initialize()
        {
            base.Initialize();
            Actor.OnHitEvent += OnHit;
        }

        public override void Reset()
        {
            base.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if(_statModule != null) _statModule.RefreshResourceValue(HealthResourceAsset);
            UpdateHealthbar();
        }
        
        public override void OnModulesInitialized()
        {
            base.OnModulesInitialized();
            _statModule = Actor.GetModule<StatsModule>();
        }

        public override void OnActorRankSet(int rank)
        {
            //Wait a frame. Because stats module needs to handle this callback as well
            StartCoroutine(RefreshAfterFrame());
        }

        private IEnumerator RefreshAfterFrame()
        {
            yield return new WaitForNextFrameUnit();
            Refresh();
        }
        
        private void OnHit(HitInfo hitInfo)
        {
            ReceiveDamage(hitInfo.DamageInfo);
        }
        
        /// <summary>
        /// Receives damage
        /// </summary>
        /// <param name="damageInfo"></param>
        public void ReceiveDamage(DamageInfo damageInfo)
        {
            if (!Actor.IsAlive()) return; //Can't kill which is already dead
            DamageInfo reducedDamage = CalculateReducedDamageInfo(damageInfo);
            float healthAfterDamage = CalculateHealthAfterDamage(reducedDamage);
            _statModule.SetResourceValue(HealthResourceAsset, healthAfterDamage);
            OnHealthChanged?.Invoke(this);

            if (ShowDamageText)
            {
                CombatManager.ShowDamageText(Actor.transform.position, reducedDamage, Actor.GetFactionId() == 0); //todo: Fix Friendly check
            }
            
            UpdateHealthbar();
            if (healthAfterDamage <= 0.0f)
            {
                Actor.KillActor();
                if (Healthbar != null)
                {
                    Healthbar.ToggleVisual(false);
                }
                if(DespawnAfterDeath)
                {
                    Actor.Despawn(DespawnDelay); // Despawn actor after delay
                }
            }
        }

        public void ReceiveHeal(DamageInfo heal)
        {
            if (!Actor.IsAlive()) return; //Can't heal the dead
            float health = GetCurrentHealth();
            float maxHealth = GetMaxHealth();
            heal.DamageAmount = Mathf.Clamp(health + heal.DamageAmount, 0, maxHealth);
            _statModule.SetResourceValue(HealthResourceAsset, heal.DamageAmount);
            OnHealthChanged?.Invoke(this);
            
            if (ShowDamageText)
            {
                CombatManager.ShowHealText(Actor.transform.position, heal, Actor.GetFactionId() == 0); //todo: Fix Friendly check
            }
            
            UpdateHealthbar();
            OnHealReceived?.Invoke(heal);
        }

        public void UpdateHealthbar()
        {
            if (Healthbar == null) return;
            float currentHealth = GetCurrentHealth();
            Healthbar.SetHealth(currentHealth, GetMaxHealth());
        }

        public DamageInfo CalculateReducedDamageInfo(DamageInfo damageInfo)
        {
            float reducedDamage = damageInfo.DamageAmount;
            if (DamageReductionFormula != null && _statModule != null && ArmorAttribute != null)
            {
                float armor = _statModule.GetAttributeValue(ArmorAttribute);
                reducedDamage *= DamageReductionFormula.GetDamageMultiplier(armor);
            }
            DamageInfo reducedDamageInfo = damageInfo;
            reducedDamageInfo.DamageAmount = reducedDamage;
            return reducedDamageInfo;
        }
        
        public float CalculateHealthAfterDamage(DamageInfo damageInfo)
        {
            float currentHealth = GetCurrentHealth();
            float damageAmount = damageInfo.DamageAmount;
            return currentHealth - damageAmount; //todo(combat): Implement armor system here
        }
        
        public float GetCurrentHealth()
        {
            return _statModule.GetResourceValue(HealthResourceAsset);
        }
        
        public float GetCurrenctPercentageHealth()
        {
            float currentHealth = GetCurrentHealth();
            float maxHealth = GetMaxHealth();
            return maxHealth > 0 ? currentHealth / maxHealth : 0f;
        }
        
        public float GetMaxHealth()
        {
            return _statModule.GetResourceMaxValue(HealthResourceAsset);
        }
    }
}