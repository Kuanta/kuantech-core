using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.Core.HyperCasual;
using Kuantech.Rpg;
using UnityEngine;

namespace Kuantech.Midcore
{
    /// <summary>
    /// A base class for progressables like skill upgrades, collectables, etc.
    /// </summary>
    [CreateAssetMenu(fileName = "ProgressableData", menuName = "Kuantech/Midcore/ProgressableData")]
    public class ProgressableDataAsset : MetadataAsset
    {
        [Header("Sub Upgrades")]
        public List<ProgressableDataAsset> SubUpgrades;
        
        [Header("Store Entry")]
        public BuyableInfo BuyableInfo;

        [Header("Progressible Level")] 
        public LevelVariableData LevelVariableData;

        [Header("Max Rank")] public int MaxRank;
        
        public static string GetSubUpgradeAssetId(ProgressableDataAsset asset, ProgressableDataAsset subUpgradeAsset)
        {
            return $"{asset.Id}_{subUpgradeAsset.Id}";
        }

        public virtual int GetMaxRank()
        {
            return MaxRank;
        }
    }
}