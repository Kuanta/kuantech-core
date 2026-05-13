using Kuantech.Core;
using UnityEngine;

namespace Kuantech.Inventory
{
    [CreateAssetMenu(fileName = "ItemData", menuName = "Kuantech/Rpg/ItemData")]
    public class ItemDataAsset : MetadataAsset
    {
        [SerializeReference] public ItemData ItemData;

        [Header("Bag Grid")]
        public int GridWidth = 1;
        public int GridHeight = 1;
    }
}