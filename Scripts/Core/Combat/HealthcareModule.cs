using Kuantech.Rpg;

namespace Kuantech.Core.Combat
{
    /// <summary>
    /// Healthcare 
    /// </summary>
    public class HealthcareModule : ActorModule
    {
        public ResourceAsset HealthResourceAsset;

        //Runtime 
        private StatsModule _statModule;
        public override void Initialize()
        {
            base.Initialize();
            Actor.OnHitEvent += OnHit;
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
            if (_statModule.GetResourceValue(HealthResourceAsset) <= 0.0f)
            {
                Actor.KillActor();
            }
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