using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Kuantech.Core.HyperCasual
{
    public enum LevelState
    {
        Waiting,
        Playing,
        Failed,
        Completed,
    }
    
    public class Level : MonoBehaviour
    {
        public LevelState CurrentState;
        public int LevelIndex;
        public List<LevelChunk> LevelChunks;
        
        //Earnings
        private Dictionary<int, Currency> _earnedCurrencies = new Dictionary<int, Currency>();

        public virtual void StartLevel()
        {
            foreach (LevelChunk chunk in LevelChunks)
            {
                chunk.OnPlay();
            }
        }

        public virtual void OnLevelCreated()
        {
            LevelChunks = GetComponentsInChildren<LevelChunk>().ToList();
            foreach (var levelChunk in LevelChunks)
            {
                levelChunk.OnLevelCreate();
            }
        }
        public virtual void PrepareLevel()
        {
            foreach (LevelChunk chunk in LevelChunks)
            {
                chunk.OnPrepare();
            }
            ReleaseEarnings();
        }

        public virtual void ClearLevel()
        {
            foreach (LevelChunk chunk in LevelChunks)
            {
                chunk.OnClear();
            }
            ReleaseEarnings();
        }

        public virtual void CompleteLevel()
        {
            SaveEarnings();
            ((HCGameManager)HCGameManager.Instance).ChangeCurrentState(LevelState.Completed);
        }

        #region Earnings

        public int GetCurrentCurrencyAmount(int currencyId)
        {
            return ((HCGameManager) HCGameManager.Instance).GetCurrency(currencyId).Amount +
                   _earnedCurrencies[currencyId].Amount;
        }
        
        /// <summary>
        /// Adds currency to the current earnings and updates the UI.
        /// Earning is not permenant and not saved to game state.
        /// Should be called from pickups
        /// </summary>
        /// <param name="currencyId"></param>
        /// <param name="amount"></param>
        public virtual void AddCurrency(int currencyId, int amount)
        {
            _earnedCurrencies ??= new Dictionary<int, Currency>();
            if (!_earnedCurrencies.ContainsKey(currencyId))
            {
                _earnedCurrencies[currencyId] = new Currency
                {
                    CurrencyId = currencyId,
                    Amount = amount
                };
                return;
            }
            _earnedCurrencies[currencyId] = _earnedCurrencies[currencyId].AddAmount(amount);
            
            //Uncomment if want to update during level
            //UIManager.Instance.SetCurrencyAmount(currencyId, GetCurrentCurrencyAmount(currencyId)); 
        }

        public virtual Currency GetEarnedCurrency(int currencyId)
        {
            if (_earnedCurrencies.ContainsKey(currencyId)) return _earnedCurrencies[currencyId];
            return new Currency
            {
                CurrencyId = currencyId,
                Amount = 0,
            };
        }
        protected virtual void SaveEarnings()
        {
            //Save currencies
            foreach (var pair in _earnedCurrencies)
            {
                ((HCGameManager)HCGameManager.Instance).AddCurrency(pair.Value.CurrencyId, pair.Value.Amount);
            }
            _earnedCurrencies.Clear();
        }
        
        protected virtual void ReleaseEarnings()
        {
            _earnedCurrencies.Clear();
        }

        #endregion
       
    }
}