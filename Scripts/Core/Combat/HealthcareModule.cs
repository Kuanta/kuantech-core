using System.Collections;
using System.Collections.Generic;
using Kuantech.Core.FX;
using Kuantech.Rpg;
using Kuantech.Utils;
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
        [Tooltip("Which resource is considered as health. Health, manages the lifecycle")]
        public ResourceAsset HealthResourceAsset;
        public List<ResourceAsset> Resources;
        
        public bool DespawnAfterDeath = true;
        [Tooltip("Delay that despawns the actor after death")] public float DespawnDelay = 1f;

        [Header("UI")] 
        [SerializeField] private Healthbar Healthbar;
        [SerializeField] private bool ShowDamageText = false;
        private Dictionary<ResourceAsset, Healthbar> _resourceBars = new Dictionary<ResourceAsset, Healthbar>();
        
        [Header("Effects")]
        [SerializeField] private Effect HealEffect;
        
        //Events
        public UnityAction<HealthcareModule> OnHealthChanged;
        public UnityAction<ResourceAsset> OnResourceChanged;
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
            if (_statModule == null) return;

            if (Resources.IsNullOrEmpty())
            {
                Debug.LogWarning($"Resources are null for {Actor.gameObject.name}");
                return;
            }
            //Refresh all resources
            foreach (var resource in Resources)
            {
                RefreshResource(resource);
            }
        }
        
        /// <summary>
        /// Refreshes a resource to its max value
        /// </summary>
        /// <param name="resource"></param>
        public void RefreshResource(ResourceAsset resource)
        {
            _statModule.RefreshResourceValue(resource);
            UpdateResourceBar(resource);
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
            //Apply main damage
            DamageResource(hitInfo.DamageInfo);
    
            //Additional damages
            if (hitInfo.AdditionalDamages != null)
            {
                foreach (var damageInfo in hitInfo.AdditionalDamages)
                {
                    DamageResource(damageInfo);
                }
            }
            
            //Check health
            float currentHealth = GetCurrentResource(HealthResourceAsset);
            if (currentHealth <= 0.0f)
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
        
        /// <summary>
        /// Receives damage
        /// </summary>
        /// <param name="damageInfo"></param>
        public void DamageResource(DamageInfo damageInfo)
        {
            if (!Actor.IsAlive()) return; //Can't kill which is already dead

            ResourceAsset resourceAsset = GetAffectedResource(damageInfo);
            DamageInfo reducedDamage = CalculateReducedDamageInfo(damageInfo);
            float resourceAfterDamage = CalculateResourceAfterDamage(reducedDamage);
            _statModule.SetResourceValue(resourceAsset, resourceAfterDamage);

            if (resourceAsset == HealthResourceAsset)
            {
                OnHealthChanged?.Invoke(this);

                if (ShowDamageText)
                {
                    CombatManager.ShowDamageText(Actor.transform.position, reducedDamage, Actor.GetFactionId() == 0); //todo: Fix Friendly check
                }
            }
            
            UpdateResourceBar(resourceAsset);

        }
        
        public void ReceiveResource(DamageInfo heal)
        {
            if (!Actor.IsAlive()) return; //Can't heal the dead
            ResourceAsset resourceAsset = heal.DamageType.AffectedResource;
            float currentRes = GetCurrentResource(resourceAsset);
            float maxRes = GetMaxResourceValue(resourceAsset);
            float newRes  = Mathf.Clamp(currentRes + heal.DamageAmount, 0, maxRes);
            
            _statModule.SetResourceValue(HealthResourceAsset, newRes);
            
            //Show heal text if health resource is increased
            if (ShowDamageText && heal.DamageType.AffectedResource == HealthResourceAsset)
            {
                CombatManager.ShowHealText(Actor.transform.position, heal, Actor.GetFactionId() == 0); //todo: Fix Friendly check
                OnHealReceived?.Invoke(heal);
                OnHealthChanged?.Invoke(this);
            }
            OnResourceChanged?.Invoke(resourceAsset);
            UpdateResourceBar(heal.DamageType.AffectedResource);
        }

        #region Resource Bars

        public void UpdateResourceBar(ResourceAsset resourceType)
        {
            Healthbar resourceBar = GetResourceBar(resourceType);
            if (resourceBar == null) return;
            float currentValue = _statModule.GetResourceValue(resourceType);
            resourceBar.SetHealth(currentValue, GetMaxResourceValue(resourceType));
        }

        public void SetResourceBar(ResourceAsset resourceType, Healthbar resourceBar)
        {
            if (_resourceBars == null) _resourceBars = new Dictionary<ResourceAsset, Healthbar>();
            _resourceBars[resourceType] = resourceBar;
        }

        public Healthbar GetResourceBar(ResourceAsset resourceType)
        {
            if (_resourceBars == null || !_resourceBars.ContainsKey(resourceType)) return null;
            return _resourceBars[resourceType];
        }

        #endregion

        public ResourceAsset GetAffectedResource(DamageInfo damageInfo)
        {
            if (damageInfo.DamageType == null) return HealthResourceAsset;
            return damageInfo.DamageType.AffectedResource;
        }
  
        /// <summary>
        /// Calculates the reduced damage info after applying armor and resistances
        /// </summary>
        /// <param name="damageInfo"></param>
        /// <returns></returns>
        public DamageInfo CalculateReducedDamageInfo(DamageInfo damageInfo)
        {
            float reducedDamage = damageInfo.DamageAmount;
            if (damageInfo.DamageType != null)
            {
                DamageReductionFormula reductionFormula = damageInfo.DamageType.DamageReductionFormula;
                AttributeAsset resistanceAttribute = damageInfo.DamageType.ResistanceAttribute;
                if (reductionFormula != null && _statModule != null && resistanceAttribute != null)
                {
                    float armor = _statModule.GetAttributeValue(resistanceAttribute);
                    reducedDamage *= reductionFormula.GetDamageMultiplier(armor);
                }
            }
       
            DamageInfo reducedDamageInfo = damageInfo;
            reducedDamageInfo.DamageAmount = reducedDamage;
            return reducedDamageInfo;
        }
        
        public float CalculateResourceAfterDamage(DamageInfo damageInfo)
        {
            float currentResource = GetCurrentResource(GetAffectedResource(damageInfo));
            float damageAmount = damageInfo.DamageAmount;
            return currentResource - damageAmount;
        }
        
        public float GetCurrentResource(ResourceAsset resourceAsset)
        {
            return _statModule.GetResourceValue(resourceAsset);
        }
        
        public float GetCurrenctPercentageResource(ResourceAsset resourceAsset)
        {
            float currentResource = GetCurrentResource(resourceAsset);
            float maxHealth = GetMaxResourceValue(resourceAsset);
            return maxHealth > 0 ? currentResource / maxHealth : 0f;
        }

        public float GetMaxResourceValue(ResourceAsset resourceAsset)
        {
            return _statModule.GetResourceMaxValue(resourceAsset);
        }
        
        public float GetCurrentHealth()
        {
            return GetCurrentResource(HealthResourceAsset);
        }

        public float GetMaxHealth()
        {
            return GetMaxResourceValue(HealthResourceAsset);
        }
    }
}