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
        public int PowerLevel = 1;
        public List<LevelChunk> LevelChunks;
        
        //Spawnables
        public List<ISpawnable> Spawns;
        
        //Earnings
        protected Dictionary<int, Currency> EarnedCurrencies = new Dictionary<int, Currency>();
    
        #region Level Lifecycke
        public virtual void StartLevel()
        {
            foreach (LevelChunk chunk in LevelChunks)
            {
                chunk.OnPlay();
            }
        }
        
        /// <summary>
        /// Called when this level is first created. Not on restarts
        /// </summary>
        public virtual void OnLevelCreated()
        {
            LevelChunks = GetComponentsInChildren<LevelChunk>().ToList();
            Spawns = GetComponentsInChildren<ISpawnable>().ToList();
            foreach (var levelChunk in LevelChunks)
            {
                levelChunk.OnLevelCreate();
            }

            foreach (ISpawnable spawnable in Spawns)
            {
                spawnable.OnSpawn();
                
            }
        }
        
        /// <summary>
        /// Prepares the level, iterates through chunks and informs about the current state
        /// </summary>
        public virtual void PrepareLevel()
        {
            foreach (LevelChunk chunk in LevelChunks)
            {
                chunk.OnPrepare(this);
            }
            ReleaseEarnings();
        }

        public virtual void RestartLevel()
        {
            foreach (LevelChunk chunk in LevelChunks)
            {
                chunk.OnRestart();
            }
            foreach (ISpawnable spawnable in Spawns)
            {
                spawnable.OnRespawn();
            }
        }
        public virtual void ClearLevel()
        {
            foreach (LevelChunk chunk in LevelChunks)
            {
                chunk.OnClear();
            }
            ReleaseEarnings();
            foreach (var spawnable in Spawns)
            {
                spawnable.OnDespawn();
            }
            Spawns.Clear();
        }

        public virtual void CompleteLevel()
        {
            ((HCGameManager)HCGameManager.Instance).ChangeCurrentState(LevelState.Completed);
            SaveEarnings();
        }
        #endregion
        
        #region Earnings

        public int GetCurrentCurrencyAmount(int currencyId)
        {
            return ((HCGameManager) HCGameManager.Instance).GetCurrency(currencyId).Amount +
                   EarnedCurrencies[currencyId].Amount;
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
            EarnedCurrencies ??= new Dictionary<int, Currency>();
            if (!EarnedCurrencies.ContainsKey(currencyId))
            {
                EarnedCurrencies[currencyId] = new Currency
                {
                    CurrencyId = currencyId,
                    Amount = amount
                };
                return;
            }
            EarnedCurrencies[currencyId] = EarnedCurrencies[currencyId].AddAmount(amount);
            
            //Uncomment if want to update during level
            //UIManager.Instance.SetCurrencyAmount(currencyId, GetCurrentCurrencyAmount(currencyId)); 
        }

        public virtual Currency GetEarnedCurrency(int currencyId)
        {
            if (EarnedCurrencies.ContainsKey(currencyId)) return EarnedCurrencies[currencyId];
            return new Currency
            {
                CurrencyId = currencyId,
                Amount = 0,
            };
        }
        protected virtual void SaveEarnings()
        {
            //Save currencies
            foreach (var pair in EarnedCurrencies)
            {
                ((HCGameManager)HCGameManager.Instance).AddCurrency(pair.Value.CurrencyId, pair.Value.Amount);
            }
            EarnedCurrencies.Clear();
        }
        
        protected virtual void ReleaseEarnings()
        {
            EarnedCurrencies.Clear();
        }

        #endregion
    }
}