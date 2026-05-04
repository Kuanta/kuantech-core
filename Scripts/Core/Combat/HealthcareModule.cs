using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using Kuantech.Core.FX;
using Kuantech.Rpg;
using Kuantech.Rpg.Managers;
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
        [SerializeField] private List<ResourceBar> ResourceBars;
        [SerializeField] private bool ShowDamageText = false;
        private Dictionary<ResourceAsset, ResourceBar> _resourceBars = new Dictionary<ResourceAsset, ResourceBar>();
        private ResourceBar Healthbar => GetResourceBar(HealthResourceAsset);
        
        [Header("Effects")]
        [SerializeField] private Effect HealEffect;

        [Header("Animations")] [SerializeField]
        private float PlayDamageThreshPercentage;
        
        //Events
        public UnityAction<HealthcareModule> OnHealthChanged;
        public UnityAction<ResourceAsset> OnResourceChanged;
        public UnityAction<float> OnHealReceived;
        public UnityAction<HitInfo> OnReceivedHitEvent; //Since we cant be sure of the order of Hit event from actor.
        
        //Runtime 
        private StatsModule _statModule;
        private AnimationModule _animationModule;
        private float _remainingDamageToPlayHitAnim;
        
        public override void Initialize()
        {
            base.Initialize();
            Actor.OnStateLoaded += OnStateLoaded;
            Actor.OnHitEvent += OnHit;
        }

        public override void OnModulesInitialized()
        {
            base.OnModulesInitialized();
            _statModule = Actor.GetModule<StatsModule>();
            _animationModule = Actor.GetModule<AnimationModule>();
            SetupResourceBars();
        }

        private void SetupResourceBars()
        {
            if(IsDedicatedServer) return;
            foreach (var resourceBar in ResourceBars)
            {
                SetResourceBar(resourceBar.ResourceAsset, resourceBar);
                UpdateResourceBar(resourceBar.ResourceAsset);
            }
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            SetupResourceBars(); //Re-update?
        }

        public override void ResetModule()
        {
            base.ResetModule();
            Refresh();
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

        private void OnStateLoaded(Actor actor)
        {
            //Update all bars
            UpdateResourceBars();
        }

        private void OnHit(HitInfo hitInfo)
        {
            if (IsServerInitialized)
            {
                float previousHealth = GetCurrentHealth();

                DamageResource(hitInfo.DamageInfo);
                if (hitInfo.AdditionalDamages != null)
                    foreach (var damageInfo in hitInfo.AdditionalDamages)
                        DamageResource(damageInfo);

                // Hit anim threshold — computed server-side where damage is authoritative
                float receivedDmg = previousHealth - GetCurrentHealth();
                _remainingDamageToPlayHitAnim -= receivedDmg;
                if (_remainingDamageToPlayHitAnim <= 0)
                {
                    SetRemainingDamageToPlayHitAnim();
                    if (IsSpawned) ObserversHitAnim_Rpc(hitInfo);
                }

                // Death check AFTER damage is applied
                if (GetCurrentHealth() <= 0.0f)
                {
                    Actor.KillActor(hitInfo.Hitter);
                    if (Healthbar != null) Healthbar.ToggleVisual(false);
                    if (DespawnAfterDeath) Actor.Despawn(DespawnDelay);
                }
            }

            OnReceivedHitEvent?.Invoke(hitInfo);
        }

        #region Resource Manipulation
        /// <summary>
        /// Reduces the resource by checking the resistance values
        /// </summary>
        /// <param name="damageInfo"></param>
        public void DamageResource(DamageInfo damageInfo)
        {
            if (!Actor.IsAlive() || !IsServerInitialized) return;
            ResourceAsset resourceAsset = GetAffectedResource(damageInfo);
            ExecuteDamageResource(damageInfo);
            if (IsSpawned) ObserverSyncResource_Rpc(resourceAsset.Id, GetCurrentResource(resourceAsset));
        }

        private void ExecuteDamageResource(DamageInfo damageInfo)
        {
            ResourceAsset resourceAsset = GetAffectedResource(damageInfo);
            DamageInfo reducedDamage = CalculateReducedDamageInfo(damageInfo);
            float resourceAfterDamage = CalculateResourceAfterDamage(reducedDamage);
            _statModule.SetResourceValue(resourceAsset, resourceAfterDamage);
            UpdateResourceBar(resourceAsset);
            if (resourceAsset == HealthResourceAsset)
            {
                OnHealthChanged?.Invoke(this);

                if (IsClientInitialized && ShowDamageText)
                {
                    CombatManager.ShowDamageText(Actor, reducedDamage); 
                }
            }
            else
            {
                OnResourceChanged?.Invoke(resourceAsset);
            }
        }

        /// <summary>
        /// Refreshes a resource to its max value
        /// </summary>
        /// <param name="resource"></param>
        public void RefreshResource(ResourceAsset resource)
        {
            if (!IsServerInitialized) return;
            ExecuteRefreshResource(resource);
            if (IsSpawned) ObserversRefreshResource_Rpc(resource.Id);
        }

        private void ExecuteRefreshResource(ResourceAsset resource)
        {
            _statModule.RefreshResourceValue(resource);
            UpdateResourceBar(resource);
        }


        /// <summary>
        /// Removes resource without applying any resistance or armor
        /// </summary>
        /// <param name="resourceAsset"></param>
        /// <param name="amount"></param>
        public void RemoveResource(ResourceAsset resourceAsset, float amount)
        {
            if (!IsServerInitialized || !Actor.IsAlive()) return;
            ExecuteRemoveResource(resourceAsset, amount);
            if (IsSpawned) ObserverSyncResource_Rpc(resourceAsset.Id, GetCurrentResource(resourceAsset));
        }

        private void ExecuteRemoveResource(ResourceAsset resourceAsset, float amount)
        {
            float currentResource = GetCurrentResource(resourceAsset);
            float newResource = Mathf.Clamp(currentResource - amount, 0, GetMaxResourceValue(resourceAsset));
            _statModule.SetResourceValue(resourceAsset, newResource);
            OnResourceChanged?.Invoke(resourceAsset);
            UpdateResourceBar(resourceAsset);
        }

        /// <summary>
        /// Adds resource
        /// </summary>
        /// <param name="heal"></param>
        public void ReceiveResource(ResourceAsset resourceAsset, float amount, bool isFriendly)
        {
            if (!IsServerInitialized || !Actor.IsAlive()) return;
            ExecuteReceiveResource(resourceAsset, amount);
            if (IsSpawned) ObserverSyncResource_Rpc(resourceAsset.Id, GetCurrentResource(resourceAsset));
        }

        private void ExecuteReceiveResource(ResourceAsset resourceAsset, float amount)
        {
            if (!Actor.IsAlive()) return; //Can't heal the dead
            float currentRes = GetCurrentResource(resourceAsset);
            float maxRes = GetMaxResourceValue(resourceAsset);
            float newRes = Mathf.Clamp(currentRes + amount, 0, maxRes);

            _statModule.SetResourceValue(HealthResourceAsset, newRes);

            //Show heal text if health resource is increased
            if (ShowDamageText && resourceAsset == HealthResourceAsset)
            {
                CombatManager.ShowHealText(Actor, amount, false);
                OnHealthChanged?.Invoke(this);
            }
            OnResourceChanged?.Invoke(resourceAsset);
            UpdateResourceBar(resourceAsset);
        }

        public void ReceiveHeal(float healAmount)
        {
            if (!IsServerInitialized || !Actor.IsAlive()) return;
            ExecuteReceiveHeal(healAmount);
            if (IsSpawned) ObserverSyncResource_Rpc(HealthResourceAsset.Id, GetCurrentHealth());
        }

        private void ExecuteReceiveHeal(float healAmount)
        {
            ExecuteReceiveResource(HealthResourceAsset, healAmount);
        }


        public void Refresh()
        {
            if (!IsServerInitialized) return;
            ExecuteRefresh();
            if (IsSpawned) ObserversRefresh_Rpc();
        }

        private void ExecuteRefresh()
        {
            if (_statModule == null) return;

            if (Resources.IsNullOrEmpty())
            {
                return;
            }
            //Refresh all resources
            foreach (var resource in Resources)
            {
                ExecuteRefreshResource(resource);
            }

            SetRemainingDamageToPlayHitAnim();
        }
        #endregion

        private void SetRemainingDamageToPlayHitAnim()
        {
            _remainingDamageToPlayHitAnim = GetMaxHealth() * Mathf.Max(0, PlayDamageThreshPercentage);
        }
        
        /// <summary>
        /// Sets the value of a resource
        /// </summary>
        /// <param name="resourceAsset"></param>
        /// <param name="value"></param>
        public void SetResourceValue(ResourceAsset resourceAsset, float value)
        {
            if (!IsServerInitialized) return;
            ExecuteSetResourceValue(resourceAsset, value);
            if (IsSpawned) ObserverSyncResource_Rpc(resourceAsset.Id, value);
        }

        private void ExecuteSetResourceValue(ResourceAsset resourceAsset, float value)
        {
            _statModule.SetResourceValue(resourceAsset, value);
            OnResourceChanged?.Invoke(resourceAsset);
            UpdateResourceBar(resourceAsset);
        }

        public void SetHealthValue(float value)
        {
            SetResourceValue(HealthResourceAsset, value);
        }

        #region Resource Bars

        public void UpdateResourceBars()
        {
            if (IsDedicatedServer) return;
            foreach (var resource in Resources)
                UpdateResourceBar(resource);
        }
        public void UpdateResourceBar(ResourceAsset resourceType)
        {
            if (IsDedicatedServer) return;
            ResourceBar resourceBar = GetResourceBar(resourceType);
            if (resourceBar == null) return;
            float currentValue = _statModule.GetResourceValue(resourceType);
            resourceBar.SetHealth(currentValue, GetMaxResourceValue(resourceType));
        }

        public void SetResourceBar(ResourceAsset resourceType, ResourceBar resourceBar)
        {
            if (IsDedicatedServer) return;

            if (_resourceBars == null) _resourceBars = new Dictionary<ResourceAsset, ResourceBar>();
            _resourceBars[resourceType] = resourceBar;
            UpdateResourceBar(resourceType);
        }

        public ResourceBar GetResourceBar(ResourceAsset resourceType)
        {
            if (_resourceBars == null || !_resourceBars.ContainsKey(resourceType)) return null;
            return _resourceBars[resourceType];
        }

        public ResourceBar GetHealhbar()
        {
            return Healthbar;
        }

        public void SetHealthbar(ResourceBar healthbar)
        {
            SetResourceBar(HealthResourceAsset, healthbar);
        }

        public void UpdateHealthbar()
        {
            UpdateResourceBar(HealthResourceAsset);
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
            float reducedDamage = damageInfo.GetDamage();
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

            DamageInfo reducedDamageInfo = new DamageInfo()
            {
                DamageType = damageInfo.DamageType,
            };
            reducedDamageInfo.SetDamage(reducedDamage);
            return reducedDamageInfo;
        }
        
        public float CalculateResourceAfterDamage(DamageInfo damageInfo)
        {
            float currentResource = GetCurrentResource(GetAffectedResource(damageInfo));
            float damageAmount = damageInfo.GetDamage();
            return currentResource - damageAmount;
        }
        
        public float GetCurrentResource(ResourceAsset resourceAsset)
        {
            return _statModule.GetResourceValue(resourceAsset);
        }
        
        public float GetCurrentPercentageResource(ResourceAsset resourceAsset)
        {
            float currentResource = GetCurrentResource(resourceAsset);
            float maxHealth = GetMaxResourceValue(resourceAsset);
            return maxHealth > 0 ? currentResource / maxHealth : 0f;
        }
        
        public float GetCurrentPercentageHealth()
        {
            return GetCurrentPercentageResource(HealthResourceAsset);
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

        public override void OnNetworkSynced()
        {
            foreach (var resource in Resources)
                UpdateResourceBar(resource);
        }

        #region Networking

        // Refresh to max — deterministic, safe to re-run on clients
        [ObserversRpc]
        private void ObserversRefreshResource_Rpc(string resourceId)
        {
            if (IsServerInitialized) return;
            ResourceAsset resourceAsset = RpgManager.GetResourceAssetById(resourceId);
            if (resourceAsset == null) return;
            ExecuteRefreshResource(resourceAsset);
        }

        [ObserversRpc]
        private void ObserversRefresh_Rpc()
        {
            if (IsServerInitialized) return;
            ExecuteRefresh();
        }

        // Authoritative value sync — server sends final value, clients just apply it
        [ObserversRpc]
        private void ObserverSyncResource_Rpc(string resourceId, float resourceValue)
        {
            if (IsServerInitialized) return;
            ResourceAsset resourceAsset = RpgManager.GetResourceAssetById(resourceId);
            if(resourceAsset == null) return;
            ExecuteSetResourceValue(resourceAsset, resourceValue);
        }

        // Hit animation — server decides when threshold is crossed, all clients play it
        [ObserversRpc]
        private void ObserversHitAnim_Rpc(HitInfo hitInfo)
        {
            if (IsServerInitialized) return;
            if (_animationModule != null) _animationModule.OnDamageReceive(hitInfo);
        }

        #endregion
    }
}