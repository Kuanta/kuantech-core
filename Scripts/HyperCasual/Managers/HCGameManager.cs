using System;
using System.Collections.Generic;
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
        //SubManagers
        [Header("SubManagers")]
        public LevelManager LevelManager; //todo: Make LevelManager a subManager
        private SubManager[] _subManagers;
        
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
        

        //Events
        public EventHandler<StateChangeData> StateChangeEvent;

        # region UnityEvents

        protected override void Start()
        {
            Initialize();
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
            //todo: Make UIManager a submanager
            GameState ??= new GameState(CurrencyIds);
            GameState.LoadData();
            EffectsLibrary.Instance.Initialize();
            foreach (var currencyId in CurrencyIds)
            {
                UIManager.Instance.SetCurrencyAmount(currencyId, GameState.GetCurrencyAmount(currencyId));
            }
            CurrentLevelIndex = GameState.GetLevelIndex();
            UIManager.Instance.SetCurrentLevel(CurrentLevelIndex);
            _lastCheckTime = Time.time;
            
            InitializeSubManagers();
            UIManager.Instance.Initialize(); //Initialize after data loading and store listing
            LevelManager = GetSubManagerByType<LevelManager>() as LevelManager;
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
            CurrentLevel.ClearLevel();
            CurrentLevel.PrepareLevel();
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
            UIManager.Instance.SetCurrentLevel(levelIndex);
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

        #region SubManagers

        public void InitializeSubManagers()
        {
            //Initialize SubManagers
            _subManagers = GetComponentsInChildren<SubManager>();
            foreach (SubManager subManager in _subManagers)
            {
                subManager.Initialize(this);
            }
        }
        
        public SubManager GetSubManagerByType<T>()
        {
            for (int i = 0; i < _subManagers.Length; i++)
            {
                if (_subManagers[i] is T)
                {
                    return _subManagers[i];
                }
            }

            return null; // Return null if no matching submanager is found
        }
        #endregion
    }
}