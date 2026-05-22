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
        public T GetItemComponentData<T>() where T : ItemComponentData
        {
            return ItemData.GetItemComponentData<T>();
        }
    }
}
