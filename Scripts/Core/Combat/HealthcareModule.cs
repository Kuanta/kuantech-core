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
        public StatAttributeAsset healthAttributeAsset;
        
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
            Actor.GetModule<StatsModule>().GetAttributeValue(healthAttributeAsset);
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