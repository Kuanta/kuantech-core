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
    public class UpgradeManager : SubManager
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
            UpgradeStateModule bsm = GameStateManager.GetModuleStatic<UpgradeStateModule>();
            if(bsm == null) return;
            foreach(var pair in _upgradesMap)
            {
                pair.Value.SetLevel(bsm.GetUpgradeLevel(pair.Key));
            }
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

        public int GetCurrentUpgradeLevel(string upgradeId)
        {
            if (_upgradesMap == null || !_upgradesMap.ContainsKey(upgradeId)) {
                return 0;
            }
            return _upgradesMap[upgradeId].CurrentLevel;
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
            string currencyId = _upgradesMap[boosterId].CurrencyData.CurrencyId;
            int currentHeldAmount = GameStateManager.GetCurrencyStatic(currencyId).Amount;
            if(price <= currentHeldAmount)
            {
                GameStateManager.GetContext<GameStateManager>().RemoveCurrency(currencyId, price);
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
            UpgradeStateModule bsm = GameStateManager.GetModuleStatic<UpgradeStateModule>();
            if(bsm == null) return;
            bsm.SetUpgradeLevel(boostId, data.CurrentLevel);
        }

        [ConsoleMethod("resetUpgrades", "Resets All Upgrades")]
        public static void ResetUpgrades()
        {
            UpgradeManager context = UpgradeManager.GetContext<UpgradeManager>();
            UpgradeStateModule bsm = GameStateManager.GetModuleStatic<UpgradeStateModule>();
            if (bsm == null) return;
            foreach(var boost in context.UpgradesList)
            {
                bsm.SetUpgradeLevel(boost.UpgradeId, 0);
                UpgradeData data = context._upgradesMap[boost.UpgradeId];
                data.CurrentLevel = 0;
                context._upgradesMap[boost.UpgradeId] = data; 
            }
            context.OnUpgradesReset?.Invoke(context, EventArgs.Empty);
        }
    }
}