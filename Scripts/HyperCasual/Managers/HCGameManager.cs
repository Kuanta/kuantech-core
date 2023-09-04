using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using IngameDebugConsole;
using Kuantech.Core.FX;
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
        //Common Submanagers
        public LevelManager LevelManager; //todo: Make LevelManager a subManager
        public UIManager UIManager;

        [Header("Loading Screen")] 
        public GameObject LoadingScreen;
        
        //Level
        [Header("Levels")]
        public int CurrentLevelIndex;
        public Level CurrentLevel;

        //Data
        [Header("Data")]
        public GameState GameState;
        [SerializeField] private float SaveCheckFrequency = 1f;
        private float _lastCheckTime;
        public List<int> CurrencyIds;
        [SerializeField] private bool SaveData = true;
        
        //Events
        public EventHandler<StateChangeData> StateChangeEvent;

        # region UnityEvents

        protected virtual void LateUpdate()
        {
            if (GameState == null) return;
            if (!(Time.time - _lastCheckTime > SaveCheckFrequency)) return;
            if(SaveData) GameState.SaveData();
            _lastCheckTime = Time.time;
        }
        
        #endregion
        
        #region Lifecycle

        protected override async UniTask Initialize()
        {
            GameState ??= new GameState(CurrencyIds);
            await GameState.LoadData();
            await base.Initialize();
        }
        
        protected override void OnSubmanagersInitialized()
        {
            base.OnSubmanagersInitialized();
            if(LoadingScreen != null) LoadingScreen.SetActive(false);
            EffectsLibrary.Instance.Initialize(); //todo(refactor): Make this a submanager

            UIManager = GetSubManagerByType<UIManager>() as UIManager;
            LevelManager = GetSubManagerByType<LevelManager>() as LevelManager;

            foreach (var currencyId in CurrencyIds)
            {
                UIManager.SetCurrencyAmount(currencyId, GameState.GetCurrencyAmount(currencyId));
            }
            CurrentLevelIndex = GameState.GetLevelIndex();
            UIManager.SetCurrentLevel(CurrentLevelIndex);
            _lastCheckTime = Time.time;
            
            UIManager.Initialize(); //Initialize after data loading and store listing
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
            if (CurrentLevel.CurrentState != LevelState.Waiting)
            {
                Debug.LogError("Trying to start level while not in waiting state");
                return;
            }
            CurrentLevel.StartLevel();
            ChangeCurrentState(LevelState.Playing);
        }

        public virtual void RestartLevel()
        {
            CurrentLevel.RestartLevel();
            ChangeCurrentState(LevelState.Waiting);
        }

        public virtual void CompleteLevel()
        {
            CurrentLevel.ClearLevel();
            Destroy(CurrentLevel.gameObject);
            CurrentLevelIndex++;
            SetLevel(CurrentLevelIndex);
            CurrentLevelIndex = CurrentLevel.LevelIndex;
            GameState.SetLevelIndex(CurrentLevelIndex);
            ChangeCurrentState(LevelState.Waiting);
        }

        public virtual void FailLevel()
        {
            ChangeCurrentState(LevelState.Failed);
        }
        public virtual void LeaveLevel()
        {
            CurrentLevel.ClearLevel();
            ChangeCurrentState(LevelState.Waiting);
        }
        [Button("SetLevel")]
        public virtual void SetLevel(int levelIndex)
        {
            levelIndex = Mathf.Max(levelIndex, 0);
            if (levelIndex == CurrentLevel.LevelIndex) return;
            if (CurrentLevel != null && CurrentLevel.LevelIndex != levelIndex)
            {
                CurrentLevel.ClearLevel();
                Destroy(CurrentLevel.gameObject);
                CurrentLevel = null;
            }

            CurrentLevelIndex = levelIndex;
            CurrentLevel = LevelManager.GetLevel(levelIndex);
            levelIndex = CurrentLevel.LevelIndex;
            CurrentLevel.PrepareLevel();
            GameState.SetLevelIndex(levelIndex);
            UIManager.SetCurrentLevel(levelIndex);
        }

        [ConsoleMethod("setLevel", "Sets the level")]
        public static void SetLevelCC(int levelIndex)
        {
            ((HCGameManager)Instance).SetLevel(levelIndex);
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
        
        [ConsoleMethod("addCurrency", "Adds Currency")]
        public static void AddCurrencyCC(int currencyId, int amount)
        {
            ((HCGameManager) Instance).AddCurrency(currencyId, amount);
        }
            
        public virtual void RemoveCurrency(int currencyId, int amount)
        {
            GameState.RemoveCurrency(currencyId, amount);
            UpdateCurrency(currencyId, GameState.GetCurrency(currencyId).Amount);
        }
        
        [Button("Set Currency")]
        public virtual void SetCurrency(int currencyId, int amount)
        {
            GameState.SetCurrency(currencyId, amount);
            UpdateCurrency(currencyId, GameState.GetCurrency(currencyId).Amount);
        }
        
        [ConsoleMethod("setCurrency", "Sets Currency")]
        public static void SetCurrencyCC(int currencyId, int amount)
        {
            ((HCGameManager) Instance).SetCurrency(currencyId, amount);
        }
        public virtual Currency GetCurrency(int currencyId)
        {
            return GameState.GetCurrency(currencyId);
        }

        public virtual void UpdateCurrency(int currencyId)
        {
            if (UIManager == null) return;
            UIManager.SetCurrencyAmount(currencyId, GameState.GetCurrencyAmount(currencyId));
        }
        
        protected virtual void UpdateCurrency(int currencyId, int amount)
        {
            if (UIManager == null) return;
            UIManager.SetCurrencyAmount(currencyId, amount);
        }
        #endregion

    
        
        #region Console Commands

        [ConsoleMethod("toggleDataSaving", "Sets data saving")]
        public static void ToggleDataSaving(int toggle)
        {
            if (toggle == 0) ((HCGameManager) HCGameManager.Instance).SaveData = false;
            else ((HCGameManager) HCGameManager.Instance).SaveData = true;
        }
        #endregion

        #region Static Methods

        public static LevelState GetCurrentLevelState()
        {
            Level currentLevel = ((HCGameManager)HCGameManager.Instance).CurrentLevel;
            if (currentLevel == null) return LevelState.Waiting;
            return currentLevel.CurrentState;
        }
        #endregion
    }
}