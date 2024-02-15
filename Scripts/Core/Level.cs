using System;
using Kuantech.Puzzle.UI;
using Kuantech.UI;
using UnityEngine;

namespace Kuantech.Core
{
    public enum LevelState
    {
        Waiting = 0,
        Playing,
        Completed,
        Failed,
    }

    public struct LevelChangeData
    {
        public LevelState OldState;
        public LevelState NewState;
    }

    public class Level : MonoBehaviour
    {
        public int LevelIndex;
        public int PowerLevel;
        private LevelState _levelState;
        public LevelState CurrentState
        {
            get {return _levelState;}
            set {
                _levelState = value;
                }
        }

        public Action<LevelChangeData> OnStateChange;

        #region Level Lifecycle
        public void ChangeLevelState(LevelState newState)
        {
            LevelState oldState = CurrentState;
            CurrentState = newState;
            OnStateChange?.Invoke(new LevelChangeData{
                OldState = oldState,
                NewState = newState,
            });
        }
        public virtual void SetupLevel()
        {
            ChangeLevelState(LevelState.Waiting);
        }

        public virtual void PlayLevel()
        {
            ChangeLevelState(LevelState.Playing);
        }

        public virtual void CompleteLevel()
        {
            if(CurrentState != LevelState.Playing) return;
            ChangeLevelState(LevelState.Completed);
        }

        public virtual void FailLevel()
        {
            if (CurrentState != LevelState.Playing) return;
            ChangeLevelState(LevelState.Failed);
        }

        public void RestartLevel()
        {
            ResetLevelState();
            PlayLevel();
        }

        //Resets all the states of the level
        public virtual void ResetLevelState()
        {
            ClearLevel();
        }
        public virtual void ClearLevel()
        {

        }
        public virtual void DestroyLevel()
        {
            Destroy(gameObject);
        }
        #endregion
    }
}