using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Kuantech.Core;
using Kuantech.Core.HyperCasual;
using Kuantech.Rpg;
using Kuantech.Utils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Kuantech.Midcore
{
    /// <summary>
    /// Defines the state for a given progression
    /// </summary>
    [Serializable]
    public class UpgradeData
    {
        public string UpgradeId;
        public int CurrentRank = -1; // < 0 means locked
    }
    
    /// <summary>
    /// Defines the requirement for a progression rank to be unlocked. If 
    /// </summary>
    [Serializable]
    public class UpgradeDependencyEntry
    {
        public UpgradeDataAsset AssetToUpgrade;
        public int RankToUpgrade;
        public UpgradeUnlockCondition UnlockCondition;
    }
    
    public class ProgressionManager : SubManager
    {
        [Header("Player Level")] 
        [SaveableField] public LevelVariable PlayerLevel;
        
        
        [Header("Upgrades")]
        public List<UpgradeDataAsset> UpgradeDataAssets;
        public List<UpgradeDependencyEntry> UpgradeDependencies;
        [SaveableField] 
        public Dictionary<string, UpgradeData> _progressionData = new Dictionary<string, UpgradeData>();
        private Dictionary<string, UpgradeDataAsset> _assetsById;
        private Dictionary<(UpgradeDataAsset, int), UpgradeUnlockCondition> _unlockConditions;

        //Events
        public Action<UpgradeData> OnUpgradeRankUp;
        public Action<LevelVariable> OnPlayerEarnedExperience;

        public override async UniTask Initialize(GameManager gameManager)
        {
            await base.Initialize(gameManager);
            _unlockConditions = new Dictionary<(UpgradeDataAsset, int), UpgradeUnlockCondition>();
            foreach (var items in UpgradeDependencies)
            {
                int rank = items.RankToUpgrade;
                UpgradeDataAsset asset = items.AssetToUpgrade;
                _unlockConditions.Add((asset, rank), items.UnlockCondition);
            }
        }
        #region Player Level
        public static LevelVariable GetPlayerLevel()
        {
            var ctx = GetContext<ProgressionManager>();
            if (ctx == null) return null;
            return ctx.PlayerLevel;
        }
        
        [Button("Add Experience")]
        public void AddExperience(int experience)
        {
            PlayerLevel.Add(experience);
            OnPlayerEarnedExperience?.Invoke(PlayerLevel);
            SaveState();
        }
        
        [Button("Set Experience")]
        public void SetExperience(int experience)
        {
            PlayerLevel.Set(experience);
            OnPlayerEarnedExperience?.Invoke(PlayerLevel);
            SaveState();
        }

        #endregion
        #region Progression Queries

        public static UpgradeData GetUpgradeData(UpgradeDataAsset asset)
        {
            var ctx = GetContext<ProgressionManager>();
            if (ctx == null) return null;
            if (ctx._progressionData.IsNullOrEmpty()) return null;
            if (!ctx._progressionData.ContainsKey(asset.Id)) return null;
            return ctx._progressionData[asset.Id];
        }

        public static UpgradeUnlockCondition GetUpgradeDependencyEntry(UpgradeDataAsset asset, int rank)
        {
            var ctx = GetContext<ProgressionManager>();
            if (ctx == null || asset == null) return null;
            ctx._unlockConditions.TryGetValue((asset, rank), out var condition);
            return condition;
        }
        
        /// <summary>
        /// Checks whether the rank is purchasable
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="rank"></param>
        /// <returns></returns>
        [Button("Check Rank Conditions")]
        public static bool IsRankUpgradeConditionsMet(UpgradeDataAsset asset, int rank)
        {
            var ctx = GetContext<ProgressionManager>();
            if(ctx == null) return false;
            //Get upgrade condition
            (UpgradeDataAsset, int) key = (asset, rank);
            if (ctx._unlockConditions != null && ctx._unlockConditions.ContainsKey(key))
            {
                UpgradeUnlockCondition condition = ctx. _unlockConditions[key];
                if (!condition.IsConditionMet()) return false;
                //Is condition satisfied?
            }
            
            //Check if previous rank is purchased
            int currentRank = GetCurrentRank(asset);
            return currentRank == rank - 1;
        }

  
        /// <summary>
        /// Checks if progression in unlocked
        /// </summary>
        /// <param name="asset"></param>
        /// <returns></returns>
        [Button("Check Upgrade Unlocked")]
        public static bool IsUpgradeUnlocked(UpgradeDataAsset asset)
        {
            return GetCurrentRank(asset) >= 0;
        }
        
        /// <summary>
        /// Checks whether the rank is unlocked
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="rankToUnlock"></param>
        /// <returns></returns>
        [Button("Check Rank Unlocked")]
        public static bool IsRankUnlocked(UpgradeDataAsset asset, int rankToUnlock)
        {
            UpgradeData data = GetUpgradeData(asset);
            if (data == null) return false;
            return data.CurrentRank >= rankToUnlock;
        }
        
        /// <summary>
        /// Checks if there is a store listing for the upgrade
        /// </summary>
        /// <param name="asset"></param>
        /// <returns></returns>
        public static bool IsUpgradeListedInStore(UpgradeDataAsset asset)
        {
            if (asset.StoreEntryId.IsNullOrEmpty() ) return false;
            StoreManager sm = GetContext<StoreManager>();
            if (sm == null) return false;
            return sm.IsListed(asset.StoreEntryId);
        }
        
        /// <summary>
        /// Returns the current rank of the progression. If the progression is locked, returns 0.
        /// </summary>
        /// <param name="asset"></param>
        /// <returns></returns>
        public static int GetCurrentRank(UpgradeDataAsset asset)
        {
            UpgradeData data = GetUpgradeData(asset);
            if (data == null) return -1;
            return data.CurrentRank;
        }

        /// <summary>
        /// Checks if there is a price for the upgrade and conditions are met
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="rank"></param>
        /// <returns></returns>
        public static bool CanBuyUpgradeRank(UpgradeDataAsset asset, int rank)
        {
            if (asset == null) return false;
            
            //Check unlock conditions
            if (IsRankUpgradeConditionsMet(asset, rank))
            {
                
            }
            if (asset.StoreEntryId.IsNullOrEmpty()) return true; //No price to pay
            StoreManager sm = StoreManager.GetContext<StoreManager>();
            if (!sm.IsListed(asset.StoreEntryId))
            {
                return true;
            }
            return sm.CanBeBought(asset.StoreEntryId, rank, GetCurrentRank(asset));
        }

        private static UpgradeData CreateUpgradeEntry(UpgradeDataAsset asset)
        {
            var ctx = GetContext<ProgressionManager>();
            if (ctx._progressionData.ContainsKey(asset.Id)) return ctx._progressionData[asset.Id];
            ctx._progressionData[asset.Id] = new UpgradeData()
            {
                CurrentRank = -1, //Start from unlocked
            };
            return ctx._progressionData[asset.Id];
        }
        #endregion

        #region Buy & Upgrade

        /// <summary>
        /// Renks up the upgrade
        /// </summary>
        /// <param name="asset"></param>
        public static bool RankUp(UpgradeDataAsset asset)
        {
            UpgradeData data = GetUpgradeData(asset);
            if (data == null) return false;
            if (!IsUpgradeUnlocked(asset))
            {
                //Possible endless recursion, be careful
                BuyRank(asset, 0);
                return false;
            }
            
            //Check price
            StoreManager sm = GetContext<StoreManager>();
            bool listedInStore = false;
            if (IsUpgradeListedInStore(asset))
            {
                //Can it be afforded
                if (!sm.CanBeBought(asset.StoreEntryId, data.CurrentRank+1, data.CurrentRank)) return false;
                listedInStore = true;
            }
            
            //Transaction
            if(listedInStore) sm.BuyItem(asset.StoreEntryId, data.CurrentRank+1, data.CurrentRank);

            //Increase the rank
            data.CurrentRank++;

            //Fire rank event
            GetContext<ProgressionManager>()?.OnUpgradeRankUp?.Invoke(data);
            return true;
        }
        
        /// <summary>
        /// Buys the rank
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="rank"></param>
        public static bool BuyRank(UpgradeDataAsset asset, int rank)
        {
            if (IsRankUnlocked(asset, rank)) return false;
            //Can be afforded?
            if (!CanBuyUpgradeRank(asset, rank))
            {
                return false;
            }
            
            UpgradeData data = GetUpgradeData(asset);
            if (data == null || !IsUpgradeUnlocked(asset))
            {
               
                //Create entry
                data = CreateUpgradeEntry(asset);
            }
            
            //Pay the price
            StoreManager.GetContext<StoreManager>().BuyItem(asset.StoreEntryId, rank, data.CurrentRank);

            data.CurrentRank = rank;
            GetContext<ProgressionManager>()?.OnUpgradeRankUp?.Invoke(data);
            var ctx = GetContext<ProgressionManager>();
            ctx.SaveState();
            return true;
        }
        #endregion
    }
}