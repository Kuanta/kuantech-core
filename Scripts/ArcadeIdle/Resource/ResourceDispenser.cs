using System;
using System.Collections.Generic;
using System.Linq;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.ArcadeIdle
{


    public class ResourceDispenser : VenueInteractable
    {
        [Header("Properties")] 
        public ResourceInventory SourceInventory;
        
        [Tooltip("Resource to dispence. If null, dispenser will try to get an available resource")]
        public ResourceFilter ResourceFilter;

        [NonSerialized] public List<ResourceData> DispensedResources;
        
        [SerializeField] private ArcadeIdleTriggerZone TriggerZone;

        public float DispenseRate = 0.1f;
        private float _lastDispensedTime = 0.0f;

        public override void Initialize()
        {
            base.Initialize();
            ResourceFilter.Initialize();
            DispensedResources = ResourceFilter.AllowedResources.ToList();
        }
        
        protected override void Update()
        {
            if(!Initialized) return;
            base.Update();
            if(TriggerZone != null)
            {
                foreach(var actor in TriggerZone.EnteredActors)
                {
                    if (actor == null || !actor.CanInteractWith(this)) continue;
                    HandleActor(actor);
                }
            }
        }
        protected override void HandleActor(ArcadeIdleCharacter character)
        {
            ResourceInventory resourceInventory = character.GetModule<ResourceInventory>();
            if (Time.time - _lastDispensedTime < DispenseRate || resourceInventory == null) return;
            List<ResourceData> availableResources = GetAvailableResourcesToDispense(character);

            for(int i=0;i<availableResources.Count;++i)
            {
                bool result = ArcadeIdleActor.TransferResource(SourceInventory,
                               resourceInventory,
                               availableResources[i],
                               true);
                if (result)
                {
                    _lastDispensedTime = Time.time;
                    return;
                }
            }
            if(character.AssignedInteractable == this) character.EndInteraction();
        }

        private List<ResourceData> GetAvailableResourcesToDispense(ArcadeIdleCharacter character)
        {
            List<ResourceData> availableResources = new List<ResourceData>();
            if(DispensedResources.IsNullOrEmpty())
            {
                availableResources = SourceInventory.GetAvailableResources();
            }else
            {
                availableResources = DispensedResources;
            }

            //Does character have a requirement list?
            RequirementList reqList = character.GetModule<RequirementList>();
            if(reqList != null  && reqList.RequiredResources != null && reqList.RequiredResources.Count > 0)
            {
                List<ResourceData> resourcesToDispense = new List<ResourceData>();

                foreach (var listElement in reqList.RequiredResources)
                {
                    if(availableResources.Contains(listElement.Key))
                    {
                        resourcesToDispense.Add(listElement.Key);
                    }
                }
                return resourcesToDispense;
            }

            return availableResources;
        }

  
        public bool CanDispenseResource(ResourceData resourceData)
        {
            return ResourceFilter.ResourceAllowed(resourceData) && SourceInventory.CanGiveResource(resourceData);
        }
    }
}