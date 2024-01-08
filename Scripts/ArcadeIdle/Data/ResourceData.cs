using Kuantech.Core;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.ArcadeIdle
{
    [CreateAssetMenu(fileName = "ResourceData", menuName = "Kuantech/ArcadeIdle/Resource")]
    public class ResourceData : ScriptableObject
    {
        public string Name;
        [KTTag("ResourceTags")]
        public int ResourceTag;
        public string ResourceId;
        public int Value = 1;
        public ResourceVisual ResourcePrefab = null;
        public Sprite ResourceIcon = null;
        public string CurrencyId = null;
        public int CurrencyAmount = 1; //A currency resource may give multiple amount of that currency
        [Tooltip("If set to true, when a resource inventory is loaded, this resource won't be added even if it was in the save state")]
        public bool PreventLoadOnInventory = false;
        
        public ResourceVisual GetResourceVisual()
        {
            if (ResourcePrefab == null) return null;
            ResourceVisual rv =  GameManager.Instance.Pool.GetObject(ResourcePrefab.gameObject).GetComponent<ResourceVisual>();
            rv.Spawn();
            rv.ResourceId = ResourceId;
            return rv;
        }

        public bool IsCurrency()
        {
            return !CurrencyId.IsNullOrEmpty();
        }
    }
}