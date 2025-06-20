using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using IngameDebugConsole;
using Kuantech.Core;

namespace Kuantech.HyperCasual
{
    
    /// <summary>
    /// Handles the pre-level boosters that give permenant upgrades to the player
    /// </summary>
    public class UpgradeManager : SubManager, ISaveable
    {
        public List<UpgradeData> UpgradesList;
        private Dictionary<string, UpgradeData> _upgradesMap;
        public EventHandler<UpgradeData> OnUpgrade;
        public EventHandler OnUpgradesReset;

        public async override UniTask Initialize(GameManager parentManager)
        {
            _upgradesMap = new Dictionary<string, UpgradeData>();
            if(UpgradesList != null)
            {
                foreach (UpgradeData upgradeData in UpgradesList)
                {
                    _upgradesMap[upgradeData.UpgradeId] = upgradeData;
                }
            }
           
            await base.Initialize(parentManager);
        }

        /// <summary>
        /// After making sure that data is loaded, set the levels for the upgrades
        /// </summary>
        public override void OnSubmanagersInitialized()
        {
            base.OnSubmanagersInitialized();
            GameStateManager.LoadData(this);
        }
        
        public bool IsMaxedOut(string upgradeId)
        {
            if (_upgradesMap == null || !_upgradesMap.ContainsKey(upgradeId)) return false;
            return(_upgradesMap[upgradeId].CurrentLevel >= _upgradesMap[upgradeId].MaxLevel);
        }

        public virtual bool CanBeUpgraded(string upgradeId)
        {
            if(IsMaxedOut(upgradeId)) return false;
            return true;
        }

        /// <summary>
        /// Returns the value of the upgrade
        /// </summary>
        /// <param name="upgradeId"></param>
        /// <returns></returns>
        public float GetUpgradeValue(string upgradeId)
        {
            if(_upgradesMap == null || !_upgradesMap.ContainsKey(upgradeId)) return 0f;
            return _upgradesMap[upgradeId].GetValue();
        }

        public static int GetCurrentUpgradeLevel(UpgradeData data)
        {
            return GetCurrentUpgradeLevel(data.UpgradeId);
        }

        public static int GetCurrentUpgradeLevel(string upgradeId)
        {
            UpgradeManager context = UpgradeManager.GetContext<UpgradeManager>();
            if(context == null) return 0;
            if (context._upgradesMap == null || !context._upgradesMap.ContainsKey(upgradeId)) {
                return 0;
            }
            return context._upgradesMap[upgradeId].CurrentLevel;
        }

        public string GetUpgradeCurrencyId(string boostId)
        {
            if (_upgradesMap == null || !_upgradesMap.ContainsKey(boostId)) return "";
            return _upgradesMap[boostId].UpgradeId;
        }

        /// <summary>
        /// Returns the price to upgrade
        /// </summary>
        /// <param name="boostId"></param>
        /// <returns></returns>
        public int GetUpgradePrice(string boostId)
        {
            if (_upgradesMap == null || !_upgradesMap.ContainsKey(boostId)) return 0;
            return _upgradesMap[boostId].GetUpgradePrice();
        }

        /// <summary>
        /// Buys an upgrade for the booster. Returns true if success.
        /// </summary>
        /// <param name="boosterId"></param>
        /// <returns></returns>
        public virtual bool BuyUpgrade(string boosterId )
        {
            //Get the price
            if(_upgradesMap == null || !_upgradesMap.ContainsKey(boosterId) || !CanBeUpgraded(boosterId)) {
                return false;
            }
            UpgradeData boostData = _upgradesMap[boosterId];
            if(boostData.CurrentLevel >= boostData.MaxLevel) 
            {
                return false;
            }
            int price = boostData.GetUpgradePrice();
            
            //Check the wallet
            bool canBuy = true;
            if(_upgradesMap[boosterId].currencyAsset != null)
            {
                string currencyId = _upgradesMap[boosterId].currencyAsset.GetId();
                int currentHeldAmount = 0; //todo(currency): Fix here
                canBuy = price <= currentHeldAmount;
                //if(canBuy) GameStateManager.GetContext<GameStateManager>().RemoveCurrency(currencyId, price);
            }
            
            if(canBuy)
            {
                PurchaseUpgrade(boosterId);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Levels up the boost and saves its data
        /// </summary>
        /// <param name="boostId"></param>
        private void PurchaseUpgrade(string boostId)
        {
            UpgradeData data = _upgradesMap[boostId];
            data.CurrentLevel = data.CurrentLevel + 1;
            _upgradesMap[boostId] = data;
            OnUpgrade?.Invoke(this, data);

            //Save to state module
            GameStateManager.UpdateSaveData(this);
        }

        [ConsoleMethod("resetUpgrades", "Resets All Upgrades")]
        public static void ResetUpgrades()
        {
            GameStateManager.ClearSaveData(UpgradeManager.GetContext<UpgradeManager>());
            UpgradeManager context = UpgradeManager.GetContext<UpgradeManager>();
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