using System;
using System.Collections.Generic;
using System.Linq;
using Kuantech.Core.UI;
using Kuantech.Utils;
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

    public struct LevelPhaseChangeData
    {
        public LevelPhase OldPhase;
        public LevelPhase NewPhase;
    }
    
    public class Level : MonoBehaviour
    {
                
        [Header("Components")] 
        public bool AutoDetectLevelElements = false;
        public List<LevelElement> LevelElements;
        
        //Runtime
        public LevelPhaseSystem PhaseSystem;
        [NonSerialized] public int LevelIndex;
        [NonSerialized] public int LevelNumber;
        [NonSerialized] public int PowerLevel;
        private LevelState _levelState;
        public LevelUI LevelUI;
        
        //Spawnables
        public HashSet<ISpawnable> SpawnedActors = new HashSet<ISpawnable>();
        
        //Level State
        public LevelState CurrentState
        {
            get {return _levelState;}
            set {
               _levelState = value;
            }
        }
        
        //Events
        public Action<LevelStateChangeData> OnStateChange; //An event bound to level.
        public Action<LevelPhaseChangeData> OnPhaseChange;
        
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
            LevelUI = UIManager.GetLevelUI();
            if (LevelUI != null)
            {
                LevelUI.OnLevelSetup(this);
            }
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
        
        /// <summary>
        /// Restarts the level
        /// </summary>
        public virtual void RestartLevel()
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
            Helpers.ResetAttributes(this);
            ClearLevel();
        }
        
        /// <summary>
        /// Clears the level
        /// </summary>
        public virtual void ClearLevel()
        {
            //Clear Spawnables
            foreach (var spawnable in SpawnedActors)
            {
                if (spawnable == null) continue;
                spawnable.Despawn(0.0f);
            }
            SpawnedActors.Clear();
        }
        
        /// <summary>
        /// Destroys the level
        /// </summary>
        public virtual void DestroyLevel()
        {
            Destroy(gameObject);
        }
        #endregion


        #region Phase Lifecycle
        
        /// <summary>
        /// Changes the level phase
        /// </summary>
        /// <param name="key"></param>
        public void ChangeLevelPhase(string key)
        {
            PhaseSystem.ChangePhase(key);
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
        

        public virtual float GetCurrentScore()
        {
            return 0f;
        }
        #endregion

        #region Spawnables

        public virtual void AddSpawnable(ISpawnable spawnable)
        {
            SpawnedActors ??= new HashSet<ISpawnable>();
            SpawnedActors.Add(spawnable);
        }
        #endregion
    }
}