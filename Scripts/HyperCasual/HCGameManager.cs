using System;
using Kuantech.Ads;
using UnityEngine;

namespace Kuantech.Core.HyperCasual
{
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
        
        //Events
        public EventHandler<LevelState> StateChangeEvent;

        # region UnityEvents
        protected override void Awake()
        {
            base.Awake();
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

        public virtual void Initialize()
        {
            //Data
            GameState = new GameState();
            GameState.LoadData();
            CurrentLevelIndex = GameState.GetLevelIndex();
            _lastCheckTime = Time.time;
            
            OnGameStart();
        }
        protected virtual void OnGameStart()
        {
            CurrentLevel = LevelManager.GetLevel(CurrentLevelIndex);
            ChangeCurrentState(LevelState.Waiting);
        }

        public virtual void PlayLevel()
        {
            CurrentLevel.StartLevel();
        }

        protected virtual void RestartLevel()
        {
            CurrentLevel.ClearLevel();
            CurrentLevel.PrepareLevel();
            CurrentLevel.StartLevel();
        }

        protected virtual void CompleteLevel()
        {
            CurrentLevel.ClearLevel();
            Destroy(CurrentLevel.gameObject);
            CurrentLevelIndex++;
            SetNextLevel(CurrentLevelIndex);
            GameState.SetLevelIndex(CurrentLevelIndex);
            
            ChangeCurrentState(LevelState.Waiting);
        }

        protected virtual void LeaveLevel()
        {
            
        }
        #endregion
        
        #region Levels

        protected virtual void SetNextLevel(int levelIndex)
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
        }

        protected virtual void SetLevel(int levelIndex)
        {
            
        }

        protected virtual void ChangeCurrentState(LevelState newState)
        {
            if (CurrentLevel == null) return;
            CurrentLevel.CurrentState = newState;
            StateChangeEvent?.Invoke(this, newState);
        }
        
        #endregion
    }
}