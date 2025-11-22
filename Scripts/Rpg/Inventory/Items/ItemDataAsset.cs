using UnityEngine;

namespace Kuantech.Rpg.Inventory
{
    [CreateAssetMenu(fileName = "ItemData", menuName = "Kuantech/Rpg/ItemData")]
    public class ItemDataAsset : ScriptableObject
    {
        [SerializeReference] public ItemData ItemData;
        public ItemVisual ItemVisual;

        public string GetItemId()
        {
            return ItemData.Id;
        }

    }
}