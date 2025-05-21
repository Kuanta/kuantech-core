using System.Collections.Generic;
using Kuantech.Core.HyperCasual;
using UnityEngine;

namespace Kuantech.Midcore
{
    /// <summary>
    /// A base class for progressables like skill upgrades, collectables, etc.
    /// </summary>
    [CreateAssetMenu(fileName = "ProgressableData", menuName = "Kuantech/MidCore/ProgressableData")]
    public class ProgressableDataAsset : ScriptableObject
    {
        public string Id;
        public string Name;
        public Sprite Icon;
        public Color Color;
        
        [Header("Sub Upgrades")]
        public List<ProgressableDataAsset> SubUpgrades;
        
        [Header("Store Entry")]
        public BuyableInfo BuyableInfo;

        public static string GetSubUpgradeAssetId(ProgressableDataAsset asset, ProgressableDataAsset subUpgradeAsset)
        {
            return $"{asset.Id}_{subUpgradeAsset.Id}";
        }
    }
}