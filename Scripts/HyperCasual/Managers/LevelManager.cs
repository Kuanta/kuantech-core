using System;
using System.Collections.Generic;
using IngameDebugConsole;
using Sirenix.OdinInspector;
using UnityEngine;
using Kuantech.Core.HyperCasual;

namespace Kuantech.Core
{

    [Serializable]
    public class LevelDictionary : SerializableDictionary<int, Level>{}
    public class LevelManager : SubManager
    {
        [Header("Levels List")] 
        public List<Level> LevelDictionary = new List<Level>();
        public Level CurrentLevel;
        public int CurrentLevelIndex;
        public int RepeatLastLevels = 0;
        public int MaxPowerLevel = -1;


        //Events
        public EventHandler<LevelStateChangeData> StateChangeEvent;
        public EventHandler<int> LevelSetEvent;
        public EventHandler<Level> LevelCompletedEvent;

        public override void OnSubmanagersInitialized()
        {
            int levelIndex = 0;
            GameStateManager gsm = (GameManager.Instance.GetSubManagerByType<GameStateManager>() as GameStateManager);
            if (gsm != null)
            {
                var module = gsm.GetModule<HyperCasualGameModel>();
                if(module != null) levelIndex = module.GetLevelIndex();
            }
            SetLevel(levelIndex);
            //ChangeCurrentState(LevelState.Waiting);
        }
        public static LevelState GetCurrentState()
        {
            LevelManager context = LevelManager.GetContext<LevelManager>();
            if(context == null || context.CurrentLevel == null) return LevelState.Waiting;
            return context.CurrentLevel.CurrentState;
        }
        public static Level GetCurrentLevel()
        {
            return (GameManager.Instance.GetSubManagerByType<LevelManager>() as LevelManager).CurrentLevel;
        }

        public static int GetCurrentLevelIndex()
        {
            return GetContext<LevelManager>().CurrentLevelIndex;
        }

        public virtual Level GetLevel(int levelIndex)
        {
            int levelArrayIndex = levelIndex;
            if (LevelDictionary.Count <= levelIndex)
            {
                if(RepeatLastLevels > 0)
                {
                    RepeatLastLevels = Mathf.Min(RepeatLastLevels, LevelDictionary.Count);
                    int modulus = RepeatLastLevels - (levelArrayIndex + 1 - LevelDictionary.Count) % RepeatLastLevels;
                    levelArrayIndex = LevelDictionary.Count - 1 - modulus;
                }else
                {
                    levelArrayIndex = LevelDictionary.Count - 1;
                }
            }

            levelArrayIndex = Mathf.Clamp(levelArrayIndex, 0, LevelDictionary.Count - 1);
            Level level = Instantiate(LevelDictionary[levelArrayIndex].gameObject).GetComponent<Level>();
            level.transform.position = Vector3.zero;
            level.transform.rotation = Quaternion.identity;
            level.LevelIndex = levelIndex;
            level.PowerLevel = levelIndex;
            return level;
        }

        /// <summary>
        /// Sets the level with the given index
        /// </summary>
        /// <param name="levelIndex"></param>
        [Button("SetLevel")]
        public void SetLevel(int levelIndex)
        {
            levelIndex = Mathf.Max(levelIndex, 0);
            CurrentLevelIndex = levelIndex;
            if (CurrentLevel != null && levelIndex == CurrentLevel.LevelIndex) return; //Don't destroy and create the same level
            if (CurrentLevel != null && CurrentLevel.LevelIndex != levelIndex)
            {
                CurrentLevel.ClearLevel();
                CurrentLevel.DestroyLevel();
                CurrentLevel = null;
            }
            CurrentLevel = GetLevel(CurrentLevelIndex);
            CurrentLevel.SetupLevel(); //todo(optimization): This may be unefficient

            //Set power level
            int powerLevel = levelIndex;
            CurrentLevel.PowerLevel = MaxPowerLevel > 0 ? Mathf.Min(MaxPowerLevel, powerLevel) : powerLevel;
            LevelSetEvent?.Invoke(this, CurrentLevelIndex);

            UpdateLevelIndex();
        }

        [ConsoleMethod("setLevel", "Sets the level")]
        public static void SetLevelCC(int levelIndex)
        {
            try{
                (GameManager.Instance.GetSubManagerByType<LevelManager>() as LevelManager).SetLevel(levelIndex);
            }catch (NullReferenceException)
            {
                Debug.LogError("Level Manager is null");
            }
        }

        [ConsoleMethod("resetLevel", "Resets the level")]
        public static void ResetLevelCC()
        {
            var context = LevelManager.GetContext<LevelManager>();
            if (context == null || context.CurrentLevel == null) return;
            context.CurrentLevel.RestartLevel();
        }
        #region Lifecycle
        public virtual void ChangeCurrentState(LevelState newState)
        {
            if (CurrentLevel == null) return;
            LevelState oldState = CurrentLevel.CurrentState;
            CurrentLevel.CurrentState = newState;
            StateChangeEvent?.Invoke(this, new LevelStateChangeData
            {
                OldState = oldState,
                NewState = newState,
            });
        }

        public void StartLevel()
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
            ChangeCurrentState(LevelState.Waiting);
            CurrentLevel.RestartLevel();
        }

        public virtual void CompleteLevel()
        {
            LevelCompletedEvent?.Invoke(this, CurrentLevel);
            CurrentLevel.ClearLevel();
            Destroy(CurrentLevel.gameObject);
            CurrentLevelIndex++;
            SetLevel(CurrentLevelIndex);
        }

        private void UpdateLevelIndex()
        {
            //Save the level index
            GameStateManager gsm = GameStateManager.GetContext<GameStateManager>();
            if (gsm != null)
            {
                var module = gsm.GetModule<HyperCasualGameModel>();
                if(module != null) module.SetLevelIndex(CurrentLevelIndex);
            }
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

        #endregion
    }
}