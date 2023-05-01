using System;
using System.Collections.Generic;
using Kuantech.Ads;
using Kuantech.UI;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Kuantech.Core.HyperCasual
{
    public struct StateChangeData
    {
        public LevelState OldState;
        public LevelState NewState;
    }
    public class HCGameManager : GameManager
    {
        public AdsManager AdsManager;
        
        //Level
        public LevelManager LevelManager;
        public int CurrentLevelIndex;
        public Level CurrentLevel;

        //Data
        public GameState GameState;
        [SerializeField] private float SaveCheckFrequency = 1f;
        private float _lastCheckTime;
        public List<int> CurrencyIds;
        
        //Events
        public EventHandler<StateChangeData> StateChangeEvent;
        //public EventHandler<Currency> CurrencyChangedEvent;

        # region UnityEvents

        protected virtual void Awake()
        {
            base.Awake();
            Initialize();
        }
        protected virtual void Update()
        {
            
        }

        protected virtual void LateUpdate()
        {
            if (!(Time.time - _lastCheckTime > SaveCheckFrequency)) return;
            GameState.SaveData();
            _lastCheckTime = Time.time;
        }
        
        #endregion
        
        #region Lifecycle

        protected virtual void Initialize()
        {
            if (GameState == null)
            {
                GameState = new GameState(CurrencyIds);
            }
            GameState.LoadData();
            foreach (var currencyId in CurrencyIds)
            {
                UIManager.Instance.SetCurrencyAmount(currencyId, GameState.GetCurrencyAmount(currencyId));
            }
            CurrentLevelIndex = GameState.GetLevelIndex();
            UIManager.Instance.HeaderPanel.SetCurrentLevel(CurrentLevelIndex);
            _lastCheckTime = Time.time;
            
            OnGameStart();
        }
        protected virtual void OnGameStart()
        {
            CurrentLevel = LevelManager.GetLevel(CurrentLevelIndex);
            CurrentLevel.PrepareLevel();
            ChangeCurrentState(LevelState.Waiting);
        }

   
        #endregion
        
        #region Levels
        
        public virtual void PlayLevel()
        {
            CurrentLevel.StartLevel();
            ChangeCurrentState(LevelState.Playing);
        }

        public virtual void RestartLevel()
        {
            CurrentLevel.ClearLevel();
            CurrentLevel.PrepareLevel();
            CurrentLevel.StartLevel();
            ChangeCurrentState(LevelState.Waiting);
        }

        public virtual void CompleteLevel()
        {
            CurrentLevel.ClearLevel();
            Destroy(CurrentLevel.gameObject);
            CurrentLevelIndex++;
            SetLevel(CurrentLevelIndex);
            GameState.SetLevelIndex(CurrentLevelIndex);
            ChangeCurrentState(LevelState.Waiting);
        }

        public virtual void FailLevel()
        {
            ChangeCurrentState(LevelState.Failed);
        }
        public virtual void LeaveLevel()
        {
            
        }
        [Button("SetLevel")]
        protected virtual void SetLevel(int levelIndex)
        {
            if (CurrentLevel != null && CurrentLevel.LevelIndex != levelIndex)
            {
                CurrentLevel.ClearLevel();
                Destroy(CurrentLevel.gameObject);
                CurrentLevel = null;
            }

            CurrentLevelIndex = levelIndex;
            CurrentLevel = LevelManager.GetLevel(levelIndex);
            CurrentLevel.PrepareLevel();
            GameState.SetLevelIndex(levelIndex);
            UIManager.Instance.HeaderPanel.SetCurrentLevel(levelIndex);
        }

        public virtual void ChangeCurrentState(LevelState newState)
        {
            if (CurrentLevel == null) return;
            LevelState oldState = CurrentLevel.CurrentState;
            CurrentLevel.CurrentState = newState;
            StateChangeEvent?.Invoke(this, new StateChangeData
            {
                OldState = oldState,
                NewState = newState,
            });
        }
        
        #endregion

        #region Currencies
        
        [Button("Add Currency")]
        public virtual void AddCurrency(int currencyId, int amount)
        {
            GameState.AddCurrency(currencyId, amount);
            UpdateCurrency(currencyId, GameState.GetCurrency(currencyId).Amount);
        }

        public virtual void RemoveCurrency(int currencyId, int amount)
        {
            GameState.RemoveCurrency(currencyId, amount);
            UpdateCurrency(currencyId, GameState.GetCurrency(currencyId).Amount);
        }

        public virtual void SetCurrency(int currencyId, int amount)
        {
            GameState.SetCurrency(currencyId, amount);
            UpdateCurrency(currencyId, GameState.GetCurrency(currencyId).Amount);
        }

        public virtual Currency GetCurrency(int currencyId)
        {
            return GameState.GetCurrency(currencyId);
        }
        
        protected virtual void UpdateCurrency(int currencyId, int amount)
        {
            UIManager.Instance.SetCurrencyAmount(currencyId, amount);
            //CurrencyChangedEvent?.Invoke(this, GameState.GetCurrency(currencyId));
        }
        #endregion
    }
}