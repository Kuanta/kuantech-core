using System;
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
        public LevelState CurrentState;

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
            ChangeLevelState(LevelState.Completed);
        }

        public virtual void FailLevel()
        {
            ChangeLevelState(LevelState.Failed);
        }

        public virtual void RestartLevel()
        {
            ChangeLevelState(LevelState.Waiting);
        }

        public virtual void ClearLevel()
        {

        }
        #endregion
    }
}