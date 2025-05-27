using Kuantech.Rpg;
using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.Core.Combat
{
    /// <summary>
    /// Healthcare 
    /// </summary>
    public class HealthcareModule : ActorModule
    {
        public ResourceAsset HealthResourceAsset;

        public bool DespawnAfterDeath = true;
        [Tooltip("Delay that despawns the actor after death")] public float DespawnDelay = 1f;
        
        [Header("UI")] 
        [SerializeField] private Healthbar Healthbar;
        
        //Events
        public UnityAction<HealthcareModule> OnHealthChanged;
        
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
            if(_statModule != null) _statModule.RefreshResourceValue(HealthResourceAsset);
            UpdateHealthbar();
        }
        
        public override void OnModulesInitialized()
        {
            base.OnModulesInitialized();
            _statModule = Actor.GetModule<StatsModule>();
        }
        
        #region Lifecycle

        

        #endregion
        
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
            float healthAfterDamage = CalculateHealthAfterDamage(damageInfo);
            _statModule.SetResourceValue(HealthResourceAsset, healthAfterDamage);
            OnHealthChanged?.Invoke(this);
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

        public void UpdateHealthbar()
        {
            if (Healthbar == null) return;
            float currentHealth = GetCurrentHealth();
            Healthbar.SetHealth(currentHealth, GetMaxHealth());
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

        public float GetMaxHealth()
        {
            return _statModule.GetResourceMaxValue(HealthResourceAsset);
        }
    }
}