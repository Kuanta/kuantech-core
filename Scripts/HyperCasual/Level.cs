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
        Paused,
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
        protected Dictionary<string, Currency> EarnedCurrencies = new Dictionary<string, Currency>();
    
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
            ReleaseEarnings();
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
            (GameManager.Instance.GetSubManagerByType<LevelManager>() as LevelManager).ChangeCurrentState(LevelState.Completed);
            SaveEarnings();
        }
        #endregion
        
        #region Earnings

        public int GetCurrentCurrencyAmount(string currencyId)
        {
            return GameStateManager.GetCurrencyStatic(currencyId).Amount +
                   EarnedCurrencies[currencyId].Amount;
        }
        
        /// <summary>
        /// Adds currency to the current earnings and updates the UI.
        /// Earning is not permenant and not saved to game state.
        /// Should be called from pickups
        /// </summary>
        /// <param name="currencyId"></param>
        /// <param name="amount"></param>
        public virtual void AddCurrency(string currencyId, int amount)
        {
            EarnedCurrencies ??= new Dictionary<string, Currency>();
            if (!EarnedCurrencies.ContainsKey(currencyId))
            {
                EarnedCurrencies[currencyId] = new Currency
                {
                    CurrencyId = currencyId,
                    Amount = amount
                };
            }
            else
            {
                EarnedCurrencies[currencyId] = EarnedCurrencies[currencyId].AddAmount(amount);
            }
        }

        public virtual Currency GetEarnedCurrency(string currencyId)
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
            GameStateManager gsm = (GameManager.Instance.GetSubManagerByType<GameStateManager>() as GameStateManager);
            foreach (var pair in EarnedCurrencies)
            {
                gsm.AddCurrency(pair.Value.CurrencyId, pair.Value.Amount);
            }
        }
        
        protected virtual void ReleaseEarnings()
        {
            GameStateManager gsm = GameStateManager.GetContext<GameStateManager>();
            List<string> currencyIds = new List<string>();
            foreach (var currency in EarnedCurrencies)
            {
                currencyIds.Add(currency.Value.CurrencyId);
            }
            EarnedCurrencies.Clear();
            // foreach (var currId in currencyIds)
            // {
            //     gsm.UpdateCurrency(currId);
            // }
        }

        #endregion
    }
}