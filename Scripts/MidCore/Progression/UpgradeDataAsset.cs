using Kuantech.Core.Store;
using UnityEngine;

namespace Kuantech.Midcore
{
    /// <summary>
    /// Metadata about the progression
    /// </summary>
    [CreateAssetMenu(fileName = "UpgradeData", menuName = "Kuantech/Progression/UpgradeData")]
    public class UpgradeDataAsset : ScriptableObject
    {
        public string Id;
        public string Name;
        public string Description;
        public Sprite Icon;
        public string StoreEntryId;
    }
}

