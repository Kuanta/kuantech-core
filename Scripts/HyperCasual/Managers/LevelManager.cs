using System;
using System.Collections.Generic;
using IngameDebugConsole;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Kuantech.Core.HyperCasual
{
    [Serializable]
    public class LevelDictionary : SerializableDictionary<int, Level>{}
    public class LevelManager : SubManager
    {
        [Header("Levels List")] 
        public List<Level> LevelDictionary = new List<Level>();
        public Level CurrentLevel;
        public int CurrentLevelIndex;


        //Events
        public EventHandler<StateChangeData> StateChangeEvent;
        public EventHandler<int> LevelSetEvent;

        public override void OnSubmanagersInitialized()
        {
            int levelIndex = 0;
            GameStateManager gsm = (GameManager.Instance.GetSubManagerByType<GameStateManager>() as GameStateManager);
            if (gsm != null)
            {
                levelIndex = gsm.GetGameState().GetLevelIndex();
            }
            SetLevel(levelIndex);
            ChangeCurrentState(LevelState.Waiting);
        }

        public virtual Level GetLevel(int levelIndex)
        {
            if (LevelDictionary.Count <= levelIndex)
            {
                levelIndex = LevelDictionary.Count - 1;
            }
            Level level = Instantiate(LevelDictionary[levelIndex].gameObject).GetComponent<Level>();
            level.transform.position = Vector3.zero;
            level.transform.rotation = Quaternion.identity;
            level.LevelIndex = levelIndex;
            level.OnLevelCreated(); //todo(optimization): This may be unefficient
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
            if (CurrentLevel != null && levelIndex == CurrentLevel.LevelIndex) return; //Don't destroy and create the same level
            if (CurrentLevel != null && CurrentLevel.LevelIndex != levelIndex)
            {
                CurrentLevel.ClearLevel();
                Destroy(CurrentLevel.gameObject);
                CurrentLevel = null;
            }
            CurrentLevelIndex = levelIndex;
            CurrentLevel = GetLevel(CurrentLevelIndex);
            CurrentLevel.PrepareLevel();
            LevelSetEvent?.Invoke(this, CurrentLevelIndex);
        }

        [ConsoleMethod("setLevel", "Sets the level")]
        public static void SetLevelCC(int levelIndex)
        {
            try{
                (GameManager.Instance.GetSubManagerByType<LevelManager>() as LevelManager).SetLevel(levelIndex);
            }catch(NullReferenceException e)
            {
                Debug.LogError("Level Manager is null");
            }
        }
        #region Lifecycle
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

        public void PlayLevel()
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

            //Save the level index
            GameStateManager gsm = (GameManager.Instance.GetSubManagerByType<GameStateManager>() as GameStateManager);
            if(gsm != null)
            {
                gsm.GetGameState().SetLevelIndex(CurrentLevelIndex);
            }
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

        #endregion
    }
}