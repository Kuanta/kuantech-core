using Kuantech.Core;
using UnityEngine;

namespace Kuantech.Inventory
{
    [CreateAssetMenu(fileName = "ItemData", menuName = "Kuantech/Inventory/ItemData")]
    public class ItemDataAsset : MetadataAsset
    {
        [SerializeField] private ItemData ItemData;

        public ItemData GetItemData()
        {
            ItemData.Id = GetId();
            ItemData.Name = GetName();
            ItemData.Description = GetDescription();
            ItemData.Icon = GetIcon();
            return ItemData;
        }

        /// <summary>
        /// Builds a display-only wrapper around an already-rolled ItemData (e.g. from a chest, which rolls by
        /// id through ItemsLibrary and never touches an ItemDataAsset) so MetadataAsset-based UI (icon/name)
        /// still has something to read.
        /// </summary>
        public void SetFromItemData(ItemData itemData)
        {
            Id = itemData.Id;
            Name = itemData.Name;
            Description = itemData.Description;
            Icon = itemData.Icon;
            ItemData = itemData;
        }
        public T GetItemComponentData<T>() where T : ItemComponentData
        {
            return ItemData.GetItemComponentData<T>();
        }
    }
}
