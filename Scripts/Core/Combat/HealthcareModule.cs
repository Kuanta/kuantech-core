using Kuantech.Rpg;
using UnityEngine;
using UnityEngine.Serialization;

namespace Kuantech.Core.Combat
{
    /// <summary>
    /// Healthcare 
    /// </summary>
    public class HealthcareModule : ActorModule
    {
        [FormerlySerializedAs("HealthAttribute")] [Header("Health Stats")] 
        public AttributeAsset healthAttributeAsset;

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
        
        public void ReceiveDamage(DamageInfo damageInfo)
        {
            if (!Actor.IsAlive()) return; //Can't kill which is already dead
            Attribute attribute = _statModule.GetAttribute(healthAttributeAsset);
            if (attribute == null)
            {
                Debug.LogWarning($"Health attribute is null for {Actor.gameObject.name}");
                return;
            }
            float healthAfterDamage = CalculateHealthAfterDamage(damageInfo);
        }

        public float CalculateHealthAfterDamage(DamageInfo damageInfo)
        {
            float currentHealth = GetCurrentHealth();
            float damageAmount = damageInfo.DamageAmount;
            return currentHealth - damageAmount; //todo(combat): Implement armor system here
        }
        
        public float GetCurrentHealth()
        {
            return Actor.GetModule<StatsModule>().GetAttributeValue(healthAttributeAsset);
        }

        public float GetMaxHealth()
        {
            return Actor.GetModule<StatsModule>().GetAttributeMaxValue(healthAttributeAsset);
        }
    }
}