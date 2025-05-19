using UnityEngine;

namespace Kuantech.Midcore
{
    [CreateAssetMenu(fileName = "CollectibleData", menuName = "Kuantech/CollectibleData", order = 0)]
    public class CollectibleDataAsset : ScriptableObject
    {
        public string CollectibleId;
        public string CollectibleName;
        public Sprite CollectibleIcon;
        public string StoreEntryId;
    }
}