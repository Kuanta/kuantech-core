using Kuantech.Core;
using UnityEngine;

namespace Kuantech.Inventory
{
    [CreateAssetMenu(fileName = "ItemData", menuName = "Kuantech/Rpg/ItemData")]
    public class ItemDataAsset : MetadataAsset
    {
        [SerializeReference] public ItemData ItemData;

        
    }
}