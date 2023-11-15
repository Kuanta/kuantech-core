using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Kuantech.Core.Utils;
using UnityEngine;

namespace Kuantech.Core.HyperCasual
{
    [Serializable]
    public class BoostData
    {
        public string BoostId;
        public int CurrentLevel;
        public int MaxLevel;
        public LeveledValueInt UpgradePrice;
        public string CurrencyId;
        public LeveledValueFloat Value;

        public float GetValue()
        {
            return Value.GetValue(CurrentLevel);
        }

        public int GetUpgradePrice()
        {
            return UpgradePrice.GetValue(CurrentLevel);
        }

        public void SetLevel(int level)
        {
            CurrentLevel = level;
        }
    }

    /// <summary>
    /// Handles the pre-level boosters that give permenant upgrades to the player
    /// </summary>
    public class BoosterManager : SubManager
    {
        public List<BoostData> BoostsList;
        private Dictionary<string, BoostData> _boostsMap;
        public EventHandler<BoostData> OnBoostUpgrade;

        public async override UniTask Initialize(GameManager parentManager)
        {
            _boostsMap = new Dictionary<string, BoostData>();
            if(BoostsList != null)
            {
                foreach (BoostData boostData in BoostsList)
                {
                    _boostsMap[boostData.BoostId] = boostData;
                }
            }
           
            await base.Initialize(parentManager);
        }

        public override void OnSubmanagersInitialized()
        {
            base.OnSubmanagersInitialized();
            BoosterStateModule bsm = GameStateManager.GetModuleStatic<BoosterStateModule>();
            if(bsm == null) return;
            foreach(var pair in _boostsMap)
            {
                pair.Value.SetLevel(bsm.GetBoostLevel(pair.Key));
            }
        }
        public bool IsMaxedOut(string boostId)
        {
            if (_boostsMap == null || !_boostsMap.ContainsKey(boostId)) return false;
            return(_boostsMap[boostId].CurrentLevel >= _boostsMap[boostId].MaxLevel);
        }

        public virtual bool CanBeUpgraded(string boostId)
        {
            if(IsMaxedOut(boostId)) return false;
            return true;
        }
        public float GetBoostValue(string boostId)
        {
            if(_boostsMap == null || !_boostsMap.ContainsKey(boostId)) return 0f;
            return _boostsMap[boostId].GetValue();
        }

        public int GetBoostLevel(string boostId)
        {
            if (_boostsMap == null || !_boostsMap.ContainsKey(boostId)) {
                return 0;
            }
            return _boostsMap[boostId].CurrentLevel;
        }

        public string GetBoostCurrencyId(string boostId)
        {
            if (_boostsMap == null || !_boostsMap.ContainsKey(boostId)) return "";
            return _boostsMap[boostId].BoostId;
        }

        public int GetBoostPrice(string boostId)
        {
            if (_boostsMap == null || !_boostsMap.ContainsKey(boostId)) return 0;
            return _boostsMap[boostId].GetUpgradePrice();
        }
        /// <summary>
        /// Buys an upgrade for the booster. Returns true if success.
        /// </summary>
        /// <param name="boosterId"></param>
        /// <returns></returns>
        public bool BuyBooster(string boosterId )
        {
            //Get the price
            if(_boostsMap == null || !_boostsMap.ContainsKey(boosterId) || !CanBeUpgraded(boosterId)) return false;
            BoostData boostData = _boostsMap[boosterId];
            if(boostData.CurrentLevel >= boostData.MaxLevel) return false;
            int price = boostData.GetUpgradePrice();
            
            //Check the wallet
            string currencyId = _boostsMap[boosterId].CurrencyId;
            int currentHeldAmount = GameStateManager.GetCurrencyStatic(currencyId).Amount;
            if(price <= currentHeldAmount)
            {
                GameStateManager.GetModuleStatic<HyperCasualGameModel>()?.RemoveCurrency(currencyId, price);
                UpgradeBoost(boosterId);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Levels up the boost and saves its data
        /// </summary>
        /// <param name="boostId"></param>
        private void UpgradeBoost(string boostId)
        {
            BoostData data = _boostsMap[boostId];
            data.CurrentLevel = data.CurrentLevel + 1;
            _boostsMap[boostId] = data;

            //Save to state module
            BoosterStateModule bsm = GameStateManager.GetModuleStatic<BoosterStateModule>();
            if(bsm == null) return;
            bsm.SetBoostLevel(boostId, data.CurrentLevel);

            OnBoostUpgrade?.Invoke(this, data);
        }
    }
}