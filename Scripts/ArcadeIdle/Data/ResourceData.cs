using System;
using Kuantech.Core;
using Kuantech.Inventory;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.ArcadeIdle
{
    [Serializable]
    public class ResourceData : ItemData
    {
        [KTTag("ResourceTags")]
        public int ResourceTag;
        public string CurrencyId = null;
        public int CurrencyAmount = 1; //A currency resource may give multiple amount of that currency
        [Tooltip("If set to true, when a resource inventory is loaded, this resource won't be added even if it was in the save state")]
        public bool PreventLoadOnInventory = false;

        
        public ResourceVisual GetResourceVisual()
        {
            ResourceVisual rv = AssetCollection.GetPrefabByType<ResourceVisual>(ItemTemplateId);
            if (rv != null)
            {
                ResourceVisual spawned = PoolManager.GetObjectFromPool(rv.gameObject).GetComponent<ResourceVisual>();
                spawned.Spawn(this);
                return spawned;
            }
            else
            {
                Debug.LogError($"No template exists with id {ItemTemplateId}");
            }
            return null;
        }

        public Sprite GetResourceIcon()
        {
            return ItemsManager.GetItemIcon(Id);
        }
        
        public bool IsCurrency()
        {
            return !CurrencyId.IsNullOrEmpty();
        }
    }
}