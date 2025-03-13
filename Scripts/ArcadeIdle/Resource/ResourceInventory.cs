using System;
using System.Collections.Generic;
using System.Linq;
using Kuantech.Core;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.ArcadeIdle
{
    [Serializable]
    public class DefaultInventoryMap : SerializableDictionary<ResourceData, int>{}

    [Serializable]
    public class ResourceInventorySerializableData : ActorModuleSerializableData
    {
        public Dictionary<string, int> HeldResources = new Dictionary<string, int>();
    }

    [Serializable]
    public class ResourceInventory : ActorModule
    {
        [Header("Inventory")]
        public List<ResourceDataReference> AcceptedResources;
        public List<ResourceDataReference> RejectedResources;
        public ResourceDisplayer ResourceDisplayer;
        public int InventoryCapacity = 2;
        public Dictionary<string, int> HeldResources = new Dictionary<string, int>();
        public Dictionary<string, int> HeldPendingResources = new Dictionary<string, int>(); //This is used for flying resources. Usable resource count: held - pending

        [Header("Default Inventory")]
        public DefaultInventoryMap DefaultInventory;

        //Events 
        public Action OnInventoryInitialized;
        public Action<(ResourceData, int)> OnResourceAdded;
        public Action<(ResourceData, int)> OnResourceRemoved;
        public Action InventoryLoaded;

        public override void SetDefaultValues()
        {
            base.SetDefaultValues();
            if(DefaultInventory == null) return;
            HeldResources = new Dictionary<string, int>();
            foreach (var inv in DefaultInventory)
            {
                for(int i=0;i<inv.Value;++i)
                {
                    AddResource(inv.Key, null, false);
                }
            }

        } 
        public override void LoadState(ActorModuleSerializableData serializableData)
        {
            base.LoadState(serializableData);
            ResourceInventorySerializableData ınventorySerializableData = serializableData as ResourceInventorySerializableData;
            HeldResources = new Dictionary<string, int>();
            if (ınventorySerializableData == null || ınventorySerializableData.HeldResources == null)
            {
                return;
            }
            foreach(var pair in ınventorySerializableData.HeldResources)
            {
                string resourceId = pair.Key;
                ResourceData data = ArcadeIdleManager.GetResourceData(resourceId);
                //Some resources are marked as not load
                if (data.PreventLoadOnInventory) continue;
                HeldResources[pair.Key] = pair.Value;
                for (int i = 0; i < pair.Value; ++i)
                {
                    ResourceVisual visual = data.GetResourceVisual();
                    AddVisual(data, visual, false);
                }
            }
        }

        public override void OnModulesInitialized()
        {
            base.OnModulesInitialized();
            InventoryLoaded?.Invoke();
        }

        protected override ActorModuleSerializableData InstantiateState()
        {
            ResourceInventorySerializableData serializableData = new ResourceInventorySerializableData();
            serializableData.HeldResources = HeldResources;
            return serializableData;
        }

        /// <summary>
        /// Checks if the inventory is full
        /// </summary>
        /// <returns></returns>
        public bool IsFull()
        {
            if(InventoryCapacity < 0) return false;
            return GetCarriedResourcesCount() >= InventoryCapacity;
        }
        
        public virtual bool CanAcceptResource(ResourceData resource)
        {
            if(resource == null)
            {
                //If resource is null, at least check if inventory is filled
                return GetCarriedResourcesCount() < InventoryCapacity && InventoryCapacity > 0 || InventoryCapacity <= 0;
            }
            if (AcceptedResources != null && AcceptedResources.Count > 0 &&
                !ResourceAccepted(resource.Id)) return false;

            //If resource isn't a currency and the inventory is full, can' add
            if (IsFull() && !resource.IsCurrency()) return false;
            return true;
        }
        
        /// <summary>
        /// Checks if the inventory can give a certain resource
        /// </summary>
        /// <param name="resource"></param>
        /// <returns></returns>
        public virtual bool CanGiveResource(ResourceData resource)
        {
            if(resource == null)
            {
                Debug.Log("There is a big issue");
                return false;
            }
            return HeldResources.ContainsKey(resource.Id) && GetAvailableAmount(resource.Id) > 0;
        }

        public List<ResourceData> GetAvailableResources()
        {
            List<ResourceData> resourceDatas = new List<ResourceData>();
            foreach (var pair in HeldResources)
            {
                ResourceData data = ArcadeIdleManager.GetResourceData(pair.Key);
                if(pair.Value > 0) resourceDatas.Add(data);
            }
            return resourceDatas;
        }

        private bool ResourceAccepted(string resourceId)
        {
            foreach (var accepted in AcceptedResources)
            {
                if (accepted.ResourceId == resourceId) return true;
            }
            return false;
        }
        
        /// <summary>
        /// Returns the available amount for a given resource. Pending resources don't count as available
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public int GetAvailableAmount(string resourceId)
        {
            if (HeldResources == null || !HeldResources.ContainsKey(resourceId)) return 0;
            //Get the pending
            int pending = HeldPendingResources != null && HeldPendingResources.ContainsKey(resourceId) ? HeldPendingResources[resourceId] : 0;
            return HeldResources[resourceId] - pending;
        }

        /// <summary>
        /// Returns the total amount of held resources. Counts both available and pending resources
        /// </summary>
        /// <param name="resourceId"></param>
        /// <returns></returns>
        public int GetHeldAmount(string resourceId)
        {
            if (HeldResources == null || !HeldResources.ContainsKey(resourceId)) return 0;
            //Get the pending
            return HeldResources[resourceId];
        }
        /// <summary>
        /// Returns the held resource count. Pending resources don't count here.
        /// </summary>
        /// <returns></returns>
        public int GetCarriedResourcesCount()
        {
            int sum = 0;
            foreach (var pair in HeldResources)
            {
                string resourceId = pair.Key;
                ResourceData data = ArcadeIdleManager.GetResourceData(resourceId);
                if(data.IsCurrency()) continue; //Currencies doesn't count as "hold" resources
                sum += pair.Value;
            }
            return sum;
        }

        public virtual void AddResource(ResourceData resourceData, ResourceVisual visual, bool flying)
        {
            if (HeldResources == null) HeldResources = new Dictionary<string, int>();
            if (!HeldResources.ContainsKey(resourceData.Id))
            {
                HeldResources[resourceData.Id] = 0;
            }

            if(HeldPendingResources == null) HeldPendingResources = new Dictionary<string, int>();
            if(!HeldPendingResources.ContainsKey(resourceData.Id))
            {
                HeldPendingResources[resourceData.Id] = 0;
            }
            DirtyState();
            if(ResourceDisplayer != null) ResourceDisplayer.OnResourceCountChanged();

            //Logical addition...
            IncreaseResourceCount(resourceData, 1, flying); //todo: Get amount

            //Handle wallet
            if(Actor != null)
            {
                ActorWallet wallet = Actor.GetModule<ActorWallet>();
                if (resourceData.IsCurrency() && wallet != null)
                {
                    wallet.DepositCurrency(resourceData.CurrencyId, resourceData.CurrencyAmount);
                }
            }

            ResourceVisual result = AddVisual(resourceData, visual, flying);
            if(result != null && flying)
            {
                result.ReachedTargetHandler = () =>{
                    if(!HeldPendingResources.ContainsKey(resourceData.Id) || HeldPendingResources[resourceData.Id] <= 0)
                    {
                        return;
                    }
                    HeldPendingResources[resourceData.Id] -= 1;
                };
            }else if(result == null && flying)
            {
                //Since we can't add the ReachedTargethandler
            }
        }

        private void IncreaseResourceCount(ResourceData resourceData, int amount, bool flying)
        {
            HeldResources[resourceData.Id] += amount;
            OnResourceAdded?.Invoke((resourceData, amount));
            if(flying)
            {
                HeldPendingResources[resourceData.Id] += amount;
            }
        }
        /// <summary>
        /// Adds a resource visual if inventory has a resource displayer
        /// </summary>
        /// <param name="resourceData"></param>
        /// <param name="visual"></param>
        /// <param name="flying"></param>
        private ResourceVisual AddVisual(ResourceData resourceData, ResourceVisual visual, bool flying)
        {
            //Visual...
            bool displayerAcceptsResource = ResourceDisplayer != null &&
                                            ResourceDisplayer.AcceptsResource(resourceData);

            if(!flying && !displayerAcceptsResource)
            {
                if(visual != null) 
                {
                    visual.Despawn();
                }
                return visual;
            }
            if (displayerAcceptsResource && visual != null)
            {
                ResourceDisplayer.AddResourceVisual(visual, flying);
            }
            else if (visual != null && flying)
            {
                //Displayer may not accept resource, but if its tagged as flying, just fly the visual to its destination. IT will be despawned thanks to following if check.
                visual.FlyToTarget(new WorldPoint()
                {
                    Target = transform,
                });
            }

            //If resource is flying with a visual, check if it should be despawned on reaching its destination
            if ((flying) && visual != null)
            {
                visual.DespawnOnReach = !displayerAcceptsResource || resourceData.IsCurrency();
                visual.ParentInventory = this;
                return visual;
            }

            //Do we need to create new visual? A generator may have generated the resource and wants to display it. Fresh baked breads...
            if (ResourceDisplayer != null && ResourceDisplayer.AcceptsResource(resourceData) && visual == null)
            {
                visual = resourceData.GetResourceVisual();
                if (visual != null)
                {
                    ResourceDisplayer.AddResourceVisual(visual, false);
                }
            }
            visual.ParentInventory = this;
            return visual;
        }

        public ResourceVisual RemoveResource(string resourceId, int amount)
        {
            if (!HeldResources.ContainsKey(resourceId)) return null;
            DirtyState();
            //Handle wallet
            ResourceData resourceData = ArcadeIdleManager.GetResourceData(resourceId);
            if(Actor != null)
            {
                ActorWallet wallet = Actor.GetModule<ActorWallet>();
                if (resourceData.IsCurrency() && wallet != null)
                {
                    wallet.WithdrawCurrency(resourceData.CurrencyId, resourceData.CurrencyAmount);
                }
            }
 
            ResourceVisual removedVisual = null;
            if (ResourceDisplayer != null)
            {
                removedVisual = ResourceDisplayer.RemoveResourceVisual(resourceId);
            }
            HeldResources[resourceId] = Mathf.Max(0, HeldResources[resourceId] - amount);
            OnResourceRemoved?.Invoke((resourceData, 1));
            if (ResourceDisplayer != null) ResourceDisplayer.OnResourceCountChanged();
            return removedVisual;
        }

        public ResourceVisual RemoveResourceVisual(string resourceId)
        {
            if(ResourceDisplayer == null) return null;
            return ResourceDisplayer.RemoveResourceVisual(resourceId);
        }

        public void ClearInventory(List<int> resourceTags)
        {
            List<string> keys = HeldResources.Keys.ToArray().ToList();
            foreach(var key in keys)
            {
                ResourceData data = ArcadeIdleManager.GetResourceData(key);
                if(data.IsCurrency() || !resourceTags.IsNullOrEmpty() && !resourceTags.Contains(data.ResourceTag)) continue;
                int amount = HeldResources[key];
                OnResourceRemoved?.Invoke((data, amount));
                for (int i=0;i<amount;++i)
                {
                    ResourceVisual visual = RemoveResourceVisual(key);
                    visual.Despawn();
                }
                HeldResources[key] = 0;
            }
            DirtyState();
        }
        public override void Reset()
        {
        }
    }
}