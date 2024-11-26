using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

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
        [NonSerialized] public int LevelIndex;
        public int LevelNumber;
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

        [FormerlySerializedAs("LevelComponents")] [Header("Components")] 
        public List<LevelElement> LevelElements;
        
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

        public virtual void OnLevelSet()
        {
            
        }
        
        public virtual void SetupLevel()
        {
            ChangeLevelState(LevelState.Waiting);
            SetupComponents();
        }

        protected virtual void SetupComponents()
        {
            foreach (var component in LevelElements)
            {
                component.ParentLevel = this;
                component.OnSetupLevel();
            }
        }
        
        /// <summary>
        /// Sets the level state to Playing. Calls the PrePlay and PostPlay for level elements
        /// </summary>
        public void StartLevel()
        {
            foreach (var component in LevelElements)
            {
                component.OnPrePlayLevel();
            }
            PlayLevel();
            foreach (var component in LevelElements)
            {
                component.OnPostPlayLevel();
            }
        }
        
        protected virtual void PlayLevel()
        {
            ChangeLevelState(LevelState.Playing);
        }

        public virtual void CompleteLevel()
        {
            if(CurrentState != LevelState.Playing) return;
            ChangeLevelState(LevelState.Completed);
            foreach (var component in LevelElements)
            {
                component.OnCompleteLevel();
            }
        }

        public virtual void FailLevel()
        {
            if (CurrentState != LevelState.Playing) return;
            ChangeLevelState(LevelState.Failed);
            foreach (var component in LevelElements)
            {
                component.OnFailLevel();
            }
        }

        public void RestartLevel()
        {
            ResetLevelState();
            StartLevel();
        }

        //Resets all the states of the level
        public virtual void ResetLevelState()
        {
            //Should this be before?
            foreach (var component in LevelElements)
            {
                component.Reset();
            }
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