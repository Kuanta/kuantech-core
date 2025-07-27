using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Kuantech.Core;
using Kuantech.Core.HyperCasual;
using Kuantech.Rpg;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Assertions;

namespace Kuantech.Midcore
{
    [Serializable]
    public struct DefautlSubUpgradeData
    {
        public ProgressableDataAsset SubUpgradeAsset;
        public int StartRank;
    }
    
    [Serializable]
    public struct DefaultProgressableData
    {
        public ProgressableDataAsset Asset;
        public int StartRank;
        public List<DefautlSubUpgradeData> SubUpgrades;
    }
    
    /// <summary>
    /// Defines a sub upgrade entry
    /// </summary>
    [Serializable]
    public struct SubUpgradeEntry
    {
        public ProgressableDataAsset ParentAsset;
        public ProgressableDataAsset SubUpgradeAsset;
    }
    
    public class ProgressionManager : SubManager
    {
        [Header("Player Level")] 
        public ProgressableDataAsset PlayerLevelDataAsset;
        
        [Header("Upgrades")] 
        [SaveableField] 
        public ProgressablesHandler ProgressiblesHandler;
        
        [Header("Level Progression")] 
        [SaveableField] public Dictionary<(int, int), LevelProgressionData> LevelProgressionDatas;

        [Header("Traits")] 
        [SerializeField] private List<TraitUpgradeProgressable> TraitUpgrades;
        
        [Header("Default Values")]
        public List<DefaultProgressableData> DefaultProgressables;
        
        //Events
        public Action<ProgressibleData> OnUpgradeUnlocked;
        public Action<ProgressibleData> OnUpgradeRankSet;
        public Action<LevelVariable> OnPlayerEarnedExperience;
        public Action<ProgressibleData> OnSubUpgradeRankSet;

        public override async UniTask Initialize(GameManager gameManager)
        {
            await base.Initialize(gameManager);
           ProgressiblesHandler.Initilaze();
           
           SetDefaultProgressables(); //Unlocks defaults if they are not unlocked
        }

        public override void SetDefaultState()
        {
            base.SetDefaultState();
            
            //Player Level
            SetRank(PlayerLevelDataAsset, 0);
            SetDefaultProgressables();
        }

        private void SetDefaultProgressables()
        {
            foreach(var defaultProgressable in DefaultProgressables)
            {
                int currRank = GetCurrentRank(defaultProgressable.Asset);
                if(currRank >= defaultProgressable.StartRank) continue;
                SetRank(defaultProgressable.Asset, defaultProgressable.StartRank, false);
                
                //Set sub upgrades too
                foreach (var upgrade in defaultProgressable.SubUpgrades)
                {
                    SetSubUpgradeRank(defaultProgressable.Asset, upgrade.SubUpgradeAsset, upgrade.StartRank, false);
                }
            }
        }
        
        #region Player Level
        public static LevelVariable GetPlayerLevel()
        {
            var ctx = GetContext<ProgressionManager>();
            if (ctx == null) return null;
            ProgressibleData data = ctx.ProgressiblesHandler.GetProgressibleData(ctx.PlayerLevelDataAsset);
            if (data == null)
            {
                SetRank(ctx.PlayerLevelDataAsset, 0);
            }
            return ctx.ProgressiblesHandler.GetProgressibleData(ctx.PlayerLevelDataAsset).GetRank();
        }
        
        [Button("Add Experience")]
        public void AddExperience(int experience)
        {
            AddRankValue(PlayerLevelDataAsset, experience);
            OnPlayerEarnedExperience?.Invoke(GetPlayerLevel());
            SaveState();
        }
        
        [Button("Set Experience")]
        public void SetExperience(int experience)
        {
            SetRankValue(PlayerLevelDataAsset, experience);
            OnPlayerEarnedExperience?.Invoke(GetPlayerLevel());
            SaveState();
        }
        
        #endregion

        #region Level Progression
        
        /// <summary>
        /// Saves a level progression data
        /// </summary>
        /// <param name="worldIndex"></param>
        /// <param name="levelIndex"></param>
        /// <param name="score"></param>
        public void SetLevelProgressionData(int worldIndex, int levelIndex, int score)
        {
            var key = (worldIndex, levelIndex);
            if (LevelProgressionDatas.ContainsKey(key))
            {
                LevelProgressionData data = LevelProgressionDatas[key];
                data.LevelScore = score;
                LevelProgressionDatas[key] = data;
            }
            else
            {
                LevelProgressionDatas.Add(key, new LevelProgressionData()
                {
                    WorldIndex = worldIndex,
                    LevelIndex = levelIndex,
                    LevelScore = score
                });
            }
        }
        
        /// <summary>
        /// Checks whether the level is completed
        /// </summary>
        /// <param name="worldIndex"></param>
        /// <param name="levelIndex"></param>
        /// <returns></returns>
        public bool IsLevelCompleted(int worldIndex, int levelIndex)
        {
            return LevelProgressionDatas.ContainsKey((worldIndex, levelIndex));
        }
        #endregion
        
        #region Progression Queries
        /// <summary>
        /// Returns the current progressible data
        /// </summary>
        /// <param name="asset"></param>
        /// <returns></returns>
        public static ProgressibleData GetProgressibleData(ProgressableDataAsset asset)
        {
            var ctx = GetContext<ProgressionManager>();
            if (ctx == null) return null;
            return ctx.ProgressiblesHandler.GetProgressibleData(asset);
        }

        public static ProgressableDependencyEntry GetProgressableDependencyEntry(ProgressableDataAsset asset, int rankToUnlock)
        {
            var ctx = GetContext<ProgressionManager>();
            if (ctx == null) return null;
            return ctx.ProgressiblesHandler.GetUpgradeDependencyEntry(asset, rankToUnlock);
        }
        
        /// <summary>
        /// Checks if progression in unlocked
        /// </summary>
        /// <param name="asset"></param>
        /// <returns></returns>
        [Button("Check Upgrade Unlocked")]
        public static bool IsProgressibleUnlocked(ProgressableDataAsset asset)
        {
            var ctx = GetContext<ProgressionManager>();
            if (ctx == null) return false;
            return ctx.ProgressiblesHandler.IsProgressibleUnlocked(asset);
        }
        
        /// <summary>
        /// Checks whether the rank is unlocked
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="rankToUnlock"></param>
        /// <returns></returns>
        [Button("Check Rank Unlocked")]
        public static bool IsRankUnlocked(ProgressableDataAsset asset, int rankToUnlock)
        {
            var ctx = GetContext<ProgressionManager>();
            if (ctx == null) return false;
            return ctx.ProgressiblesHandler.IsRankUnlocked(asset, rankToUnlock);
        }

        public static bool IsRankConditionSatisfied(ProgressableDataAsset asset, int rankToUnlock)
        {
            var ctx = GetContext<ProgressionManager>();
            if (ctx == null) return false;
            return ctx.ProgressiblesHandler.CanRankBeUnlocked(asset, rankToUnlock);
        }
        
        /// <summary>
        /// Returns the current rank of the progression. If the progression is locked, returns 0.
        /// </summary>
        /// <param name="asset"></param>
        /// <returns></returns>
        public static int GetCurrentRank(ProgressableDataAsset asset, ProgressableDataAsset subAsset = null)
        {
            var ctx = GetContext<ProgressionManager>();
            if (ctx == null) return -1;
            if (subAsset != null)
            {
                return ctx.ProgressiblesHandler.GetSubUpgradeRank(asset, subAsset);
            }
            return ctx.ProgressiblesHandler.GetCurrentRank(asset);
        }

        /// <summary>
        /// Checks if there is a price for the upgrade and conditions are met
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="rank"></param>
        /// <returns></returns>
        public static bool CanRankBeUnlocked(ProgressableDataAsset asset, int rank)
        {
            var ctx = GetContext<ProgressionManager>();
            return ctx.ProgressiblesHandler.CanRankBeUnlocked(asset, rank);
        }

        public override void ClearState()
        {
            ProgressiblesHandler.Clear(); //Clear upgradables
            base.ClearState();
        }
        #endregion

        #region Buy & Upgrade

        /// <summary>
        /// Ranks up the upgrade, checks its price
        /// </summary>
        /// <param name="asset"></param>
        public static bool RankUpUpgrade(ProgressableDataAsset asset)
        {
            var ctx = GetContext<ProgressionManager>();
            if (!ctx.ProgressiblesHandler.RankUp(asset)) return false;
            ctx.OnUpgradeRankSet?.Invoke(ctx.ProgressiblesHandler.GetProgressibleData(asset));
            ctx.SaveState();
            return true;
        }
        
        /// <summary>
        /// Buys the rank
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="rank"></param>
        [Button("Buy Rank")]
        public static bool BuyRank(ProgressableDataAsset asset, int rank)
        {
            var ctx = GetContext<ProgressionManager>();
            if (!ctx.ProgressiblesHandler.BuyRank(asset, rank)) return false;
            ctx.OnUpgradeRankSet?.Invoke(ctx.ProgressiblesHandler.GetProgressibleData(asset));
            ctx.SaveState();
            return true;
        }
        
        /// <summary>
        /// Sets the rank without any checks
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="rank"></param>
        /// <returns></returns>
        [Button("Set Rank")]
        public static bool SetRank(ProgressableDataAsset asset, int rank, bool saveState = true)
        {
            var ctx = GetContext<ProgressionManager>();
            if (ctx == null) return false;
            bool result=ctx.ProgressiblesHandler.SetRank(asset, rank);
            if (!result) return false;
            ctx.OnUpgradeRankSet?.Invoke(ctx.ProgressiblesHandler.GetProgressibleData(asset));
            if (saveState)
            {
                ctx.SaveState();
            }

            return true;
        }
        
        [Button("Add Rank Experience")]
        public static bool AddRankValue(ProgressableDataAsset asset, float rankValue, bool saveState = true)
        {
            var ctx = GetContext<ProgressionManager>();
            if (ctx == null) return false;
            bool result = ctx.ProgressiblesHandler.AddRankValue(asset, rankValue);
            if (!result) return false;
            ctx.OnUpgradeRankSet?.Invoke(ctx.ProgressiblesHandler.GetProgressibleData(asset));
            if (saveState)
            {
                ctx.SaveState();
            }

            return true;
        }

        [Button("Set Rank Experience")]
        public static bool SetRankValue(ProgressableDataAsset asset, float rankValue, bool saveState = true)
        {
            var ctx = GetContext<ProgressionManager>();
            if (ctx == null) return false;
            bool result = ctx.ProgressiblesHandler.SetRankValue(asset, rankValue);
            if (!result) return false;
            ctx.OnUpgradeRankSet?.Invoke(ctx.ProgressiblesHandler.GetProgressibleData(asset));
            if (saveState)
            {
                ctx.SaveState();
            }

            return true;
        }
        
        [Button("Buy Sub Upgrade Rank")]
        public static bool BuySubUpgradeRank(ProgressableDataAsset parentAsset, ProgressableDataAsset subUpgrade,
            int rank)
        {
            var ctx = GetContext<ProgressionManager>();
            if (!ctx.ProgressiblesHandler.BuySubUpgradeRank(parentAsset, subUpgrade, rank)) return false;
            ctx.OnSubUpgradeRankSet?.Invoke(ctx.ProgressiblesHandler.GetSubUpgradeData(parentAsset,subUpgrade));
            ctx.SaveState();
            return true;
        }

        /// <summary>
        /// Sets the sub upgrade rank without any checks
        /// </summary>
        /// <param name="parentAsset"></param>
        /// <param name="subUpgrade"></param>
        /// <param name="rank"></param>
        /// <returns></returns>
        [Button("Set Sub Upgrade Rank")]
        public static bool SetSubUpgradeRank(ProgressableDataAsset parentAsset, ProgressableDataAsset subUpgrade,
            int rank, bool saveState=true)
        {
            var ctx = GetContext<ProgressionManager>();
            if (ctx == null) return false;
            if (!ctx.ProgressiblesHandler.SetSubUpgradeRank(parentAsset, subUpgrade, rank)) return false;
            ctx.OnSubUpgradeRankSet?.Invoke(ctx.ProgressiblesHandler.GetSubUpgradeData(parentAsset,subUpgrade));
            ctx.SaveState();
            return true;
        }

        #endregion

        #region Trait Upgrades

        public static void ApplyTraitUpgradesToActor(Actor actor)
        {
            var ctx = GetContext<ProgressionManager>();
            if (ctx == null) return;
            if (ctx.TraitUpgrades == null || ctx.TraitUpgrades.Count == 0) return;

            foreach (var traitUpgrade in ctx.TraitUpgrades)
            {
                if (traitUpgrade == null) continue;
                traitUpgrade.ApplyToActor(actor);
            }            
        }
        #endregion

    }
}