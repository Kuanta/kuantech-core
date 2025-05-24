using System;
using System.Collections.Generic;
using System.Linq;
using Kuantech.Utils;
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

    public struct LevelPhaseChangeData
    {
        public LevelPhase OldPhase;
        public LevelPhase NewPhase;
    }
    
    public class Level : MonoBehaviour
    {
        //Runtime
        public LevelPhaseSystem PhaseSystem;
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
        public Action<LevelPhaseChangeData> OnPhaseChange;
        
        [Header("Components")] 
        public bool AutoDetectLevelElements = false;
        public List<LevelElement> LevelElements;
        
        #region Level Lifecycle
        //A simple relayer to LevelManager
        public virtual void ChangeLevelState(LevelState newState)
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
            SetupPhaseSystem();
            ChangeLevelState(LevelState.Waiting);
            SetupComponents();
        }

        protected virtual void SetupPhaseSystem()
        {
            PhaseSystem = new LevelPhaseSystem(this);
        }
        protected virtual void SetupComponents()
        {
            if (AutoDetectLevelElements)
            {
                LevelElements = GetComponentsInChildren<LevelElement>().ToList();
            }
            foreach (var component in LevelElements)
            {
                component.ParentLevel = this;
                component.OnSetupLevel();
            }
        }

        #region Level Lifecycle

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


        protected virtual void Update()
        {
            if (CurrentState != LevelState.Playing) return;
            if (PhaseSystem != null && PhaseSystem.CurrentPhase != null)
            {
                PhaseSystem.CurrentPhase.TickPhase(Time.deltaTime);
            }
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

        public virtual void RestartLevel()
        {
            ResetLevelState();
            StartLevel();
        }

        #endregion


        #region Phase Lifecycle
        
        /// <summary>
        /// Changes the level phase
        /// </summary>
        /// <param name="key"></param>
        public void ChangeLevelPhase(string key)
        {
            LevelPhase oldPhase = PhaseSystem.CurrentPhase;
            PhaseSystem.ChangePhase(key);
            LevelPhase newPhase = PhaseSystem.CurrentPhase;
            
            OnPhaseChange?.Invoke(new LevelPhaseChangeData
            {
                OldPhase = oldPhase,
                NewPhase = newPhase,
            });
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public LevelPhase GetCurrentPhase()
        {
            return PhaseSystem.CurrentPhase;
        }
        #endregion
        
        //Resets all the states of the level
        public virtual void ResetLevelState()
        {
            //Should this be before?
            foreach (var component in LevelElements)
            {
                component.Reset();
            }
            Helpers.ResetAttributes(this);
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