using System;
using System.Collections.Generic;
using System.Linq;
using Kuantech.ArcadeIdle.UI;
using Kuantech.Core;
using UnityEngine;

namespace Kuantech.ArcadeIdle
{
    [Serializable]
    public struct RequirementListTemplate
    {
        public List<RequirementListEntry> Requirements;
    }
    [Serializable]
    public struct RequirementListEntry
    {
        public ResourceData ResourceData;
        public int RequiredAmount;
        [NonSerialized] public int GatheredAmount;
        [NonSerialized] public bool PaidFor;
    }

    /// <summary>
    /// A requirement list is a collection of desired resources. 
    /// </summary>
    public class RequirementList : ActorModule
    {
        public ResourceInventory SourceInventory;
        public List<RequirementListEntry> ShoppingListEntriesList;
        public Dictionary<ResourceData, RequirementListEntry> RequiredResources = new Dictionary<ResourceData, RequirementListEntry>();
        public RequirementListIndicator Indicator;
        public override void Initialize()
        {
            RequiredResources = new Dictionary<ResourceData, RequirementListEntry>();
            foreach (var entry in ShoppingListEntriesList)
            {
                RequiredResources[entry.ResourceData] = entry;
            }
        }

        public void ToggleIndicator(bool toggle)
        {
            if(Indicator == null) return;
            Indicator.gameObject.SetActive(toggle);
        }

        public void InitializeIndicator()
        {
            if(Indicator == null) return;
            Indicator.Setup(this);
        }

        public void UpdateResourceInIndicator(ResourceData data)
        {
            if(Indicator == null) return;
            int remainingAmount = RequiredResources[data].RequiredAmount - RequiredResources[data].GatheredAmount;
            Indicator.UpdateResourceAmount(data, remainingAmount);
        }

        public void SetGatheredAmount(ResourceData data, int gatheredAmount)
        {
            RequirementListEntry entry = RequiredResources[data];
            entry.GatheredAmount = gatheredAmount;
            RequiredResources[data] = entry;
        }
        /// <summary>
        /// Adds a resource to the requirement list
        /// </summary>
        /// <param name="data">Resource data</param>
        /// <param name="amount">Amount of resource to require. -1 means as many</param>
        public void AddToShoppingList(ResourceData data, int amount = 1)
        {
            if (RequiredResources.ContainsKey(data))
            {
                RequirementListEntry entry = RequiredResources[data];
                entry.ResourceData = data;
                entry.RequiredAmount += amount;
                entry.GatheredAmount = 0;
                entry.PaidFor = false;
                return;
            }
            RequiredResources[data] = new RequirementListEntry()
            {
                ResourceData = data,
                RequiredAmount = amount,
                PaidFor = false,
                GatheredAmount = 0,
            };
        }

        /// <summary>
        /// Checks if the required resources are gathered
        /// </summary>
        /// <returns></returns>
        public bool IsResourcesGathered(ResourceData data)
        {
            if(!RequiredResources.ContainsKey(data)) return true;
            if(SourceInventory == null)
            {
                Debug.LogError($"Resource Inventory for shopping list of {Actor.name} is null");
                return false;
            }
            int heldAmount = SourceInventory.GetHeldAmount(data.Id);
            return heldAmount >= RequiredResources[data].RequiredAmount && RequiredResources[data].RequiredAmount > 0; //If required amount is <0, can never be fully gathered
        }

        public bool AreResourcesGathered()
        {
            foreach(var reqRes in RequiredResources)
            {
                if(!IsResourcesGathered(reqRes.Key)) return false;
            }
            return true;
        }

        /// <summary>
        /// Empties out the list
        /// </summary>
        public void EmptyList()
        {
            RequiredResources.Clear();
        }
        public override void Reset()
        {
            List<ResourceData> keys = RequiredResources.Keys.ToList();
            foreach (var key in keys)
            {
                RequirementListEntry entry = RequiredResources[key];
                entry.GatheredAmount = 0;
                entry.PaidFor = false;
                RequiredResources[key] = entry;
            }
        }
    }
}