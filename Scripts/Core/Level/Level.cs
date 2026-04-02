using System;
using System.Collections.Generic;
using System.Linq;
using Kuantech.Core.UI;
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

    public struct LevelIdentificationData
    {
        public string WorldID;
        public int WorldIndex;
        public int LevelIndex;
    }
    
    public class Level : MonoBehaviour
    {
        [Header("Common Properties")]
        public bool AutoStartAfterSetup = false;
        
        [Header("Components")] 
        public bool AutoDetectLevelElements = false;
        public List<LevelElement> LevelElements;

        [FormerlySerializedAs("UseWorldIndex")]
        [Header("Analytics")] 
        [Tooltip("Should world index be sent to analytics")]
        public bool TriggerEventWithWorldIndex = false;
        [Tooltip("Should event with linear level number be triggered")]
        public bool TriggerEventWithLinearLevelNumber = true;
        
        //Runtime
        public LevelPhaseSystem PhaseSystem;
        [NonSerialized] public int LevelIndex;
        [NonSerialized] public int LevelNumber;
        private LevelState _levelState;
        public LevelUI LevelUI;
        
        //World Data
        [NonSerialized] public WorldDataAsset WorldDataAsset = null;
        [NonSerialized] public int WorldIndex; //Index in the levels array. 
        [NonSerialized] public int WorldNumber; //Actual world number. Number of world the player is at. There may be 5 worlds but player can be at 50th world. Used to provide infinite gameplay
        
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
        public Action<LevelStateChangeData> OnStateChangeEvent; //An event bound to level.
        public Action<LevelPhaseChangeData> OnPhaseChangeEvent;
        
        #region Level Lifecycle
        //A simple relayer to LevelManager
        public virtual void ChangeLevelState(LevelState newState)
        {
            LevelManager levelman = LevelManager.GetContext<LevelManager>();
            if (levelman == null)
            {
                Debug.LogError("Level Manager is null, can't change level state");
            }

            LevelStateChangeData levelStateChangeData = new LevelStateChangeData
            {
                OldState = CurrentState,
                NewState = newState,
            };
            
            foreach (var module in Modules.Values)
            {
                module.OnLevelStateChange(levelStateChangeData);
            }
            
            //For subscribers that subscribe to level only
            OnStateChangeEvent?.Invoke(levelStateChangeData);
            CurrentState = newState;
            //Inform level manager
            levelman.ChangeCurrentState(newState);
        }

        public virtual void OnLevelSet()
        {
            
        }
        
        public virtual void SetupLevel()
        {
            LevelUI = UIManager.GetLevelUI();
            DetectModules();
            if (LevelUI != null)
            {
                LevelUI.OnLevelSetup(this);
            }
            SetupPhaseSystem();
            SetupComponents();
            ChangeLevelState(LevelState.Waiting);
            
            //Call post level setup
            foreach(var module in LevelModules)
            {
                module.PostLevelSetup();
            }
            
                        
            //Trigger analytics
            if (TriggerEventWithWorldIndex)
            {
                Analytics.Analytics.OnWorldLevelStarted(WorldNumber, LevelIndex);
            }
            if(TriggerEventWithLinearLevelNumber)
            {
                Analytics.Analytics.OnLevelStarted(GetLevelNumber());
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
            
            //Trigger analytics
            if (TriggerEventWithWorldIndex)
            {
                Analytics.Analytics.OnWorldLevelEnded(WorldNumber, LevelIndex, true, GetCurrentScore());
            }
            if(TriggerEventWithLinearLevelNumber)
            {
                Analytics.Analytics.OnLevelEnded(GetLevelNumber(), true, GetCurrentScore());
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
            
            //Trigger analytics
            if (TriggerEventWithWorldIndex)
            {
                Analytics.Analytics.OnWorldLevelEnded(WorldNumber, LevelIndex, false, GetCurrentScore());
            }
            if(TriggerEventWithLinearLevelNumber )
            {
                Analytics.Analytics.OnLevelEnded(GetLevelNumber(), false, GetCurrentScore());
            }
        }
        
        /// <summary>
        /// Quits from current level
        /// </summary>
        public virtual void QuitLevel()
        {
            ClearLevel();
            
            //Trigger analytics
            if (TriggerEventWithWorldIndex)
            {
                Analytics.Analytics.OnWorldLevelEnded(WorldNumber, LevelIndex, false, GetCurrentScore());
            }
            if(TriggerEventWithLinearLevelNumber)
            {
                Analytics.Analytics.OnLevelEnded(GetLevelNumber(), false, GetCurrentScore());
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
            
            //Reset level modules
            foreach (var module in Modules.Values)
            {
                module.OnReset();
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

            foreach (var module in Modules.Values)
            {
                module.OnLevelClear();
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

        public virtual void OnLevelPhaseChange(LevelPhase oldPhase, LevelPhase newPhase)
        {
            foreach (var module in LevelModules)
            {
                module.OnLevelPhaseChange(oldPhase, newPhase);    
            }
            
            OnPhaseChangeEvent?.Invoke(new LevelPhaseChangeData()
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
        
        #endregion

        #region Spawnables

        public virtual void AddSpawnable(ISpawnable spawnable)
        {
            SpawnedActors ??= new HashSet<ISpawnable>();
            SpawnedActors.Add(spawnable);
        }
        #endregion

        #region Modules
        //Level Modules
        protected List<LevelModule> LevelModules = new List<LevelModule>();
        protected Dictionary<Type, LevelModule> Modules = new Dictionary<Type, LevelModule>();

        protected virtual void DetectModules()
        {
            LevelModules = GetComponentsInChildren<LevelModule>().ToList();
            foreach (LevelModule lm in LevelModules)
            {
                Modules[lm.GetType()] = lm;
                lm.ParentLevel = this;
            }

            foreach (var lm in LevelModules)
            {
                lm.Initialize();
            }
        }
        
        public T GetLevelModule<T>() where T : LevelModule
        {
            foreach (var pair in Modules)
            {
                if (pair.Value is T)
                {
                    return pair.Value as T;
                }
            }
            return null;
        }
        #endregion
        
        #region Level Info
        
        /// <summary>
        /// Returns the number of level. Not array index, the number
        /// </summary>
        /// <returns></returns>
        public int GetLevelNumber()
        {
            int levelNumber = LevelNumber;
            if (WorldDataAsset != null)
            {
                levelNumber = LevelManager.GetContext<LevelManager>().GetTotalLevelIndex(WorldNumber, LevelIndex);
            }
            return levelNumber;
        }
        
        public virtual float GetCurrentScore()
        {
            return 0f;
        }

        public virtual int GetPowerLevel()
        {
            return GetLevelNumber();
        }
        #endregion

    }
}