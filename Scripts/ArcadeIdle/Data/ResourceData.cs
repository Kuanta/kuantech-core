using Kuantech.Core;
using Kuantech.Rpg;
using Kuantech.Rpg.Inventory;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.ArcadeIdle
{
    public class ResourceData : ItemData
    {
        public string Name;
        [KTTag("ResourceTags")]
        public int ResourceTag;
        public int Value = 1;
        public string CurrencyId = null;
        public int CurrencyAmount = 1; //A currency resource may give multiple amount of that currency
        [Tooltip("If set to true, when a resource inventory is loaded, this resource won't be added even if it was in the save state")]
        public bool PreventLoadOnInventory = false;

        
        public ResourceVisual GetResourceVisual()
        {
            GameObject gameObject = Librarian.GetItemVisualPrefab(Id);
            if (gameObject.TryGetComponent(out ResourceVisual rv))
            {
                PoolManager.GetObjectFromPool(rv.gameObject).GetComponent<ResourceVisual>();
            }
            return null;
        }

        public Sprite GetResourceIcon()
        {
            return Librarian.GetItemIcon(Id);
        }
        
        public bool IsCurrency()
        {
            return !CurrencyId.IsNullOrEmpty();
        }
    }
}