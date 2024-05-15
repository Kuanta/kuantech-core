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

    public struct LevelStateChangeData
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

        public Action<LevelStateChangeData> OnStateChange; //An event bound to level.

        #region Level Lifecycle
        //A simple relayer to LevelManager
        public void ChangeLevelState(LevelState newState)
        {
            LevelManager levelman = LevelManager.GetContext<LevelManager>();
            if (levelman == null)
            {
                Debug.LogError("Level Manager is null, can't change level state");
            }
            //For subscribers that subscribe to level only
            OnStateChange?.Invoke(new LevelStateChangeData
            {
                OldState = CurrentState,
                NewState = newState,
            });
            
            //Inform level manager
            levelman.ChangeCurrentState(newState);
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
        public virtual float GetCurrentScore()
        {
            return 0f;
        }
        #endregion
    }
}