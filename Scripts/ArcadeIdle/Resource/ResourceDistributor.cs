using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.ArcadeIdle
{
    [Serializable]
    public struct ResourceToInventoryPair
    {
        public ResourceData ResourceData;
        public ResourceInventory Inventory;
    }

    public class ResourceDistributor : ResourceInventory
    {
        [SerializeField] private List<ResourceToInventoryPair> TargetInventories;
        private Dictionary<ResourceData, ResourceInventory> _inventoryMapping;

        public override void Initialize()
        {
            base.Initialize();
            _inventoryMapping = new Dictionary<ResourceData, ResourceInventory>();
            foreach(var pair in TargetInventories)
            {
                _inventoryMapping[pair.ResourceData] = pair.Inventory;
            }
        }

        public override bool CanAcceptResource(ResourceData resource)
        {
            if(resource == null)
            {
                return false;
            }
            if(!_inventoryMapping.ContainsKey(resource)) return false;
            return _inventoryMapping[resource].CanAcceptResource(resource);
        }

        public override bool CanGiveResource(ResourceData resource)
        {
            return false;
        }

        public override void AddResource(ResourceData resourceData, ResourceVisual visual, bool flying)
        {
            ResourceInventory mappedInventory = _inventoryMapping[resourceData];
            mappedInventory.AddResource(resourceData, visual, flying);
        }
    }
}