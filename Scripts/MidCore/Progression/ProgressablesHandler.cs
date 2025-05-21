using System;
using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.Core.HyperCasual;
using Kuantech.Rpg;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Midcore
{
    /// <summary>
    /// A single conidition that requires the rank of another progressable to be unlocked.
    /// </summary>
    [Serializable]
    public class ProgressableUnlockCondition
    {
        public ProgressableDataAsset DependingAsset;
        public int DependingProgressionRank;
    }
    
    [Serializable]
    public class ProgressableDependencyEntry
    {
        public ProgressableDataAsset AssetToUpgrade;
        public int RankToUpgrade;
        public int RequiredPlayerRank;
        public List<ProgressableUnlockCondition> UnlockConditions;
    }
    
    [Serializable]
    public class ProgressablesHandler : ISaveable
    {
        [Header("Dependencies")]
        public List<ProgressableDependencyEntry> UpgradeDependencies;
        
        [SaveableField] private Dictionary<string, ProgressibleData> _progressibleDatas;
        private Dictionary<(ProgressableDataAsset, int), ProgressableDependencyEntry> _unlockConditions;

        public void Initilaze()
        {
            _unlockConditions = new Dictionary<(ProgressableDataAsset, int), ProgressableDependencyEntry>();
            _progressibleDatas = new Dictionary<string, ProgressibleData>();
            foreach (var items in UpgradeDependencies)
            {
                int rank = items.RankToUpgrade;
                ProgressableDataAsset asset = items.AssetToUpgrade;
                _unlockConditions.Add((asset, rank), items);
            }
        }
        
        /// <summary>
        /// Creates a progressible data entry with 0 rank
        /// </summary>
        /// <param name="asset"></param>
        /// <returns></returns>
        protected virtual ProgressibleData CreateDataEntry(ProgressableDataAsset asset)
        {
            //Create data entry
            ProgressibleData data = new ProgressibleData()
            {
                Id = asset.Id,
                Rank = new LevelVariable(),
            };
            CreateSubProgessibleDatas(asset);
            return data;
        }
        
        protected virtual void CreateSubProgessibleDatas(ProgressableDataAsset parentAsset)
        {
            if (parentAsset.SubUpgrades.IsNullOrEmpty()) return;
            foreach(var subUpgrade in parentAsset.SubUpgrades)
            {
                string id = ProgressableDataAsset.GetSubUpgradeAssetId(parentAsset, subUpgrade);
                _progressibleDatas[id] = CreateSubUpgradeData(parentAsset, subUpgrade);
            }
        }

        private ProgressibleData CreateSubUpgradeData(ProgressableDataAsset parentAsset, ProgressableDataAsset asset)
        {
            ProgressibleData data = CreateDataEntry(asset);
            data.ParentProgressibleId = parentAsset.Id;
            data.Id = ProgressableDataAsset.GetSubUpgradeAssetId(parentAsset, asset);
            data.Rank.SetLevel(0);
            return data;
        }
        #region Queries
        public ProgressibleData GetProgressibleData(ProgressableDataAsset asset)
        {
            return GetProgessibleDataById(asset.Id);
        }

        public ProgressibleData GetProgessibleDataById(string id)
        {
            if (_progressibleDatas.IsNullOrEmpty() || 
                !_progressibleDatas.ContainsKey(id)) return null;
            return _progressibleDatas[id];
        }
        
        /// <summary>
        /// Checks whether the progressable has at least a single rank, unlocked
        /// </summary>
        /// <param name="asset"></param>
        /// <returns></returns>
        public bool IsProgressibleUnlocked(ProgressableDataAsset asset)
        {
            return GetProgressibleData(asset) != null;
        }
        
        /// <summary>
        /// returns the rank of the progressable
        /// </summary>
        /// <param name="asset"></param>
        /// <returns></returns>
        public int GetCurrentRank(ProgressableDataAsset asset)
        {
            ProgressibleData data = GetProgressibleData(asset);
            if (data == null) return -1;
            return data.Rank.CurrentLevel;
        }
        public bool IsRankUnlocked(ProgressableDataAsset asset, int rank)
        {
            ProgressibleData data = GetProgressibleData(asset);
            if (data == null) return false;
            return data.Rank.CurrentLevel >= rank;
        }
        public int GetProgressibleRank(ProgressableDataAsset asset)
        {
            ProgressibleData data = GetProgressibleData(asset);
            if (data == null) return -1;
            return data.Rank.CurrentLevel;
        }
        
        /// <summary>
        /// Checks for conditions to be met for the rank to be unlocked. Doesn't check for price
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="rank"></param>
        /// <returns></returns>
        public bool CanRankBeUnlocked(ProgressableDataAsset asset, int rank)
        {
            ProgressableDependencyEntry dependencyEntry = GetUpgradeDependencyEntry(asset, rank);
            if (dependencyEntry == null) return true;
            
            //Check player level
            int playerLevel = ProgressionManager.GetPlayerLevel().CurrentLevel;
            if(dependencyEntry.RequiredPlayerRank > playerLevel) return false;
            
            //Check other conditions
            if (!dependencyEntry.UnlockConditions.IsNullOrEmpty())
            {
                foreach (var condition in dependencyEntry.UnlockConditions)
                {
                    if (!UnlockConditionSatisfied(condition)) return false;
                }
            }
            return true;
        }

        protected bool UnlockConditionSatisfied(ProgressableUnlockCondition condition)
        {
            int rankOfCondition = GetProgressibleRank(condition.DependingAsset);
            if(rankOfCondition < condition.DependingProgressionRank) return false;
            return true;
        }
        public ProgressableDependencyEntry GetUpgradeDependencyEntry(ProgressableDataAsset asset, int rank)
        {
            if (_unlockConditions.IsNullOrEmpty()) return null;
            if (_unlockConditions.ContainsKey((asset, rank)))
                return _unlockConditions[(asset, rank)];
            return null;
        }
        
        /// <summary>
        /// Checks if the rank can be purchased. This does not check if the rank is already purchased.
        /// </summary>
        /// <param name="asset"></param>
        /// <returns></returns>
        public bool CanBeAfforded(ProgressableDataAsset asset, int rank, int startRank)
        {
           StoreManager sm = StoreManager.GetContext<StoreManager>();
           if (sm == null) return true; //No stroe manager
           if (asset.BuyableInfo.Id.IsNullOrEmpty() && asset.BuyableInfo.PricesInfo.IsNullOrEmpty()) return true; //No buyable info
           return sm.CanBeBought(asset.BuyableInfo, rank, startRank); //Rank is used to calculate the price
        }
        
        /// <summary>
        /// Unlocks the progressable. Simply sets its rank to 0 if its locked
        /// </summary>
        /// <param name="asset"></param>
        /// <returns>Returns success if unlocked. returns false if already unlocked</returns>
        public bool UnlockProgressable(ProgressableDataAsset asset)
        {
            if (IsProgressibleUnlocked(asset)) return false;
            return SetRank(asset, 0);
        }
        
        /// <summary>
        /// Sets the rank of the progressable. If the rank is 0, it will unlock the progressable. Doesn't check for conditions or prices.
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="rank"></param>
        /// <returns></returns>
        public virtual bool SetRank(ProgressableDataAsset asset, int rank)
        {
            ProgressibleData data = GetProgressibleData(asset);
            if (data == null)
            {
                data = CreateDataEntry(asset);
                _progressibleDatas[asset.Id] = data;
            }
            data.Rank.SetLevel(rank);
            return true;
        }

        /// <summary>
        /// Tries to buy the rank. Checks price and dependency conditions and pays for the price
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="rank"></param>
        /// <returns></returns>
        public virtual bool BuyRank(ProgressableDataAsset asset, int rank)
        {
            if (!CanRankBeUnlocked(asset, rank)) return false;
            int currentRank = GetCurrentRank(asset);
            if (!CanBeAfforded(asset, rank, currentRank)) return false;
            
            //Money upfront
            PayThePrice(asset, rank, currentRank);

            SetRank(asset, rank);
            return true;
        }
        /// <summary>
        /// Pays the price 
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="rankToBuy"></param>
        /// <param name="startRank"></param>
        public void PayThePrice(ProgressableDataAsset asset, int rankToBuy, int startRank)
        {
            StoreManager sm = StoreManager.GetContext<StoreManager>();
            if (sm == null) return;
            sm.BuyItem(asset.BuyableInfo, rankToBuy, startRank);
        }
        
        /// <summary>
        /// Increases the rank by one. Checks for price and conditions
        /// </summary>
        /// <param name="asset"></param>
        /// <returns></returns>
        public bool RankUp(ProgressableDataAsset asset)
        {
            ProgressibleData data = GetProgressibleData(asset);
            if (data == null)
            {
                return BuyRank(asset, 1); //This unlocks
            }

            return BuyRank(asset, data.Rank.CurrentLevel+1);
        }
        #endregion
        
        #region Sub Upgrades
        
        /// <summary>
        /// Returns subupgrade data
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="subUpgradeAsset"></param>
        /// <returns></returns>
        public ProgressibleData GetSubUpgradeData(ProgressableDataAsset asset, ProgressableDataAsset subUpgradeAsset)
        {
            return GetProgessibleDataById(ProgressableDataAsset.GetSubUpgradeAssetId(asset, subUpgradeAsset));
        }
        
        /// <summary>
        /// Returns the subupgrade rank.
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="subUpgradeAsset"></param>
        /// <returns></returns>
        public int GetSubUpgradeRank(ProgressableDataAsset asset, ProgressableDataAsset subUpgradeAsset)
        {
            ProgressibleData data = GetProgessibleDataById(ProgressableDataAsset.GetSubUpgradeAssetId(asset, subUpgradeAsset));
            if (data == null) return 0; //Default rank for a subupgrade is 0. Not -1, cause its not something we unlock (for now).
            return data.Rank.CurrentLevel;
        }
        
        /// <summary>
        /// Returns the level of subupgrade
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="subUpgradeAsset"></param>
        /// <returns></returns>
        public int GetSubUpgradeLevel(ProgressableDataAsset asset, ProgressableDataAsset subUpgradeAsset)
        {
            ProgressibleData subUpgradeData = GetSubUpgradeData(asset, subUpgradeAsset);
            if (subUpgradeData == null) return 0;
            return subUpgradeData.Rank.CurrentLevel;
        }

        /// <summary>
        /// Ranks up the sub upgrade. Checks for price and conditions
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="subUpgradeAsset"></param>
        /// <returns></returns>
        public bool RankUpSubUpgrade(ProgressableDataAsset asset, ProgressableDataAsset subUpgradeAsset)
        {
            int currentrank = GetSubUpgradeRank(asset, subUpgradeAsset);
            return BuySubUpgradeRank(asset, subUpgradeAsset, currentrank + 1);
        }
        
        /// <summary>
        /// Sets the rank of sub upgrade
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="subUpgradeAsset"></param>
        /// <param name="rank"></param>
        /// <returns></returns>
        public bool SetSubUpgradeRank(ProgressableDataAsset asset, ProgressableDataAsset subUpgradeAsset, int rank)
        {
            ProgressibleData subUpgradeData = GetSubUpgradeData(asset, subUpgradeAsset);
            if (subUpgradeData == null)
            {
                subUpgradeData = CreateSubUpgradeData(asset, subUpgradeAsset);
            }
            subUpgradeData.Rank.SetLevel(rank);
            return true;
        }
        
        /// <summary>
        /// Buts the given rank for subupgrade
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="subUpgradeAsset"></param>
        /// <param name="rank"></param>
        /// <returns></returns>
        public bool BuySubUpgradeRank(ProgressableDataAsset asset, ProgressableDataAsset subUpgradeAsset, int rank)
        {
            int currentRank = GetSubUpgradeRank(asset, subUpgradeAsset);
            if (!CanBeAfforded(subUpgradeAsset, rank, currentRank)) return false;
            PayThePrice(subUpgradeAsset, rank, currentRank);
            SetSubUpgradeRank(asset, subUpgradeAsset, rank);
            return true;
        }
        #endregion
        
        public void Clear()
        {
            _progressibleDatas.Clear();
        }
        public byte[] Serialize()
        {
            return null;
        }

        public void Deserialize(byte[] data)
        {
            
        }
    }
}