using UnityEngine;

namespace Kuantech.Rpg.Inventory
{
    [CreateAssetMenu(fileName = "ItemData", menuName = "Kuantech/Rpg/ItemData")]
    public class ItemDataAsset : ScriptableObject
    {
        public ItemData ItemData;

        public string GetItemId()
        {
            return ItemData.Id;
        }
    }
}