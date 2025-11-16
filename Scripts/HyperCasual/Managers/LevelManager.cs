using System;
using System.Collections.Generic;
using IngameDebugConsole;
using Kuantech.Midcore;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Kuantech.Core
{
    [Serializable]
    public class LevelManager : SubManager
    {
        [Header("Properties")] [SerializeField]
        private bool SetNextLevelAfterComplete;
        
        [Header("Levels List")] 
        public List<Level> LevelDictionary = new List<Level>();
        public List<WorldDataAsset> Worlds = new List<WorldDataAsset>();
        public Level CurrentLevel;
        
        public int CurrentLevelIndex;
        public int CurrentWorldIndex;
        public int RepeatLastLevels = 0;
        public int MaxPowerLevel = -1;

        //Events
        public EventHandler<LevelStateChangeData> StateChangeEvent;
        public EventHandler<int> LevelSetEvent;
        public EventHandler<Level> LevelCompletedEvent;

        #region Submanager OVerrides

        public override void OnSceneLeave()
        {
            base.OnSceneLeave();
            ClearCurrentLevel(); //Clear current level
        }

        #endregion
        public static LevelState GetCurrentState()
        {
            LevelManager context = LevelManager.GetContext<LevelManager>();
            if(context == null || context.CurrentLevel == null) return LevelState.Waiting;
            return context.CurrentLevel.CurrentState;
        }
        
        public static Level GetCurrentLevel()
        {
            var ctx = LevelManager.GetContext<LevelManager>();
            if (ctx == null) return null;
            return ctx.CurrentLevel;
        }

        public static int GetCurrentLevelIndex()
        {
            return GetContext<LevelManager>().CurrentLevelIndex;
        }

        public int GetWorldIndex(int worldIndex)
        {
            int worldArrayIndex = worldIndex;                                                                
            if (Worlds.Count <= worldArrayIndex)                                                             
            {                                                                                                
                if (RepeatLastLevels > 0)                                                                    
                {                                                                                            
                    RepeatLastLevels = Mathf.Min(RepeatLastLevels, Worlds.Count);                            
                    int modulus = RepeatLastLevels - (worldArrayIndex + 1 - Worlds.Count) % RepeatLastLevels;
                    worldArrayIndex = Worlds.Count - 1 - modulus;                                            
                }                                                                                            
                else                                                                                         
                {                                                                                            
                    worldArrayIndex = Worlds.Count - 1;                                                      
                }                                                                                            
            }                                                                                                
                                                                                                 
            return Mathf.Clamp(worldArrayIndex, 0, Worlds.Count - 1);                             
        }
        public virtual WorldDataAsset GetWorld(int worldIndex)
        {
            if (worldIndex < 0) worldIndex = 0;
            int worldArrayIndex = GetWorldIndex(worldIndex);
            WorldDataAsset worldAsset = Worlds[worldArrayIndex];
            return worldAsset;
        }

        #region World Levels
        
        /// <summary>
        /// Returns flattened level index
        /// </summary>
        /// <param name="worldIndex"></param>
        /// <param name="levelIndex"></param>
        /// <returns></returns>
        public int GetTotalLevelIndex(int worldNumber, int levelIndex)
        {
            int totalLevels = 0;
            if (worldNumber <= 0) return levelIndex;
            for (int i = 0; i < worldNumber; ++i)
            {
                totalLevels += GetWorld(i).Levels.Count;
            }
            return totalLevels + levelIndex + 1;
        }
        
        public Level GetWorldLevelPrefab(int worldIndex, int levelIndex)
        {
            WorldDataAsset worldDataAsset = GetWorld(worldIndex);
            if (worldDataAsset == null) return null;
            Level levelPrefab = worldDataAsset.GetLevelPrefab(levelIndex);
            if(levelPrefab == null) return null;
            return levelPrefab;
        }
        
        [Button("Set World Level")]
        public void SetWorldLevel(int worldIndex, int levelIndex)
        {
            CurrentWorldIndex = GetWorldIndex(worldIndex);
            WorldDataAsset worldDataAsset = GetWorld(worldIndex);
            levelIndex = Mathf.Clamp(levelIndex, 0, worldDataAsset.Levels.Count);
            CurrentLevelIndex = levelIndex;
            if (CurrentLevel != null && CurrentLevel.WorldIndex == worldIndex && CurrentLevelIndex == levelIndex) return;
            if (CurrentLevel != null)
            {
                ClearCurrentLevel();
            }
            Level levelPrefab = worldDataAsset.GetLevelPrefab(levelIndex);
            Level level = InstantiateLevel(levelPrefab);
            CurrentLevel = level;
            CurrentLevel.WorldIndex = CurrentWorldIndex;
            CurrentLevel.WorldNumber = Mathf.Max(worldIndex, 0);
            CurrentLevel.WorldDataAsset = worldDataAsset;
            CurrentLevel.LevelIndex = levelIndex;
            CurrentLevel.LevelNumber = levelIndex;
            CurrentLevel.OnLevelSet();
            CurrentLevel.SetupLevel();    
        }

        public LevelIndexData GetCorrectedLevelIndex(LevelIndexData levelIndexData)
        {
            int levelIndex = levelIndexData.LevelIndex;
            int worldIndex = levelIndexData.WorldIndex;

            worldIndex = GetWorldIndex(worldIndex);
            WorldDataAsset worldDataAsset = GetWorld(worldIndex);
            if (levelIndex >= worldDataAsset.Levels.Count)
            {
                levelIndex = 0;
                worldIndex += 1;
            }

            return new LevelIndexData()
            {
                WorldIndex = worldIndex,
                LevelIndex = levelIndex,
            };
        }
        
        #endregion
        
        #region Flat Levels List

        public virtual Level GetLevelPrefab(int levelIndex)
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
            return LevelDictionary[levelArrayIndex];
        }

        /// <summary>
        /// Returns the array index in an array given the level index, array size and repeat count
        /// </summary>
        /// <param name="levelIndex">Unbounded level index. For example 1000th level</param>
        /// <param name="arraySize">Size of levels list</param>
        /// <param name="repeatCount">How many levels to repeat at the end of level array</param>
        /// <param name="powerLevel">Power level, means the iteration count.(Count of repeat) </param>
        /// <returns></returns>
        [Button("Get ArrayIndex")]
        public static int GetArrayIndexFromLevelIndex(int levelIndex, int arraySize, int repeatCount, out int powerLevel)
        {
            powerLevel = 0;
            if (levelIndex < arraySize) return levelIndex;

            if (repeatCount <= 0)
            {
                return arraySize - 1;
            }

            repeatCount = Mathf.Clamp(repeatCount, 0, arraySize);
            
            int a = levelIndex - arraySize;
            powerLevel = 1 + Mathf.FloorToInt(a / (float) repeatCount);
            int remainder = a % repeatCount;
            return remainder + (arraySize - repeatCount);
        }
        
        /// <summary>
        /// Sets the level with the given index
        /// </summary>
        /// <param name="levelIndex"></param>
        [Button("SetLevel")]
        public void SetLevel(int levelIndex, bool clearLevelState = false)
        {
            levelIndex = Mathf.Max(levelIndex, 0);
            CurrentLevelIndex = levelIndex;
            if (CurrentLevel != null && levelIndex == CurrentLevel.LevelIndex) return; //Don't destroy and create the same level
            if (CurrentLevel != null && CurrentLevel.LevelIndex != levelIndex)
            {
                ClearCurrentLevel();
            }

            Level levelPrefab = GetLevelPrefab(levelIndex);
            Level level = InstantiateLevel(levelPrefab);
            
            //Usefull for repeting last x levels. If there are 20 levels, and we are trying to get 41th level,
            //this value will be the index of the corresponding repeated level in the levels array
            level.LevelIndex = levelIndex;
            CurrentLevel = level;

            //Set power level
            int powerLevel = levelIndex;
            CurrentLevel.LevelNumber = MaxPowerLevel > 0 ? Mathf.Min(MaxPowerLevel, powerLevel) : powerLevel;
            LevelSetEvent?.Invoke(this, CurrentLevelIndex);
            if(clearLevelState) CurrentLevel.OnLevelSet();
            CurrentLevel.SetupLevel(); //todo(optimization): This may be unefficient
            UpdateLevelIndex();
        }
        
        /// <summary>
        /// Instantiates and positions a level
        /// </summary>
        /// <param name="levelPrefab"></param>
        public Level InstantiateLevel(Level levelPrefab)
        {
            Level level = Instantiate(levelPrefab.gameObject).GetComponent<Level>();  
            level.transform.position = Vector3.zero;                
            level.transform.rotation = Quaternion.identity;
            return level;
        }
        
        public void ClearCurrentLevel()
        {
            if (CurrentLevel == null) return;
            CurrentLevel.ClearLevel();
            CurrentLevel.DestroyLevel();
            CurrentLevel = null;
        }
        #endregion

        [ConsoleMethod("setLevel", "Sets the level")]
        public static void SetLevelCC(int levelIndex)
        {
            GetContext<LevelManager>().SetLevel(levelIndex, true);
        }

        [ConsoleMethod("resetLevel", "Resets the level")]
        public static void ResetLevelCC()
        {
            var context = LevelManager.GetContext<LevelManager>();
            if (context == null || context.CurrentLevel == null) return;
            context.CurrentLevel.RestartLevel();
        }
        
        #region Level - Lifecycle
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

            if (SetNextLevelAfterComplete)
            {
                SetNextLevel();
            }
        }
        
        /// <summary>
        /// Sets the next level
        /// </summary>
        public virtual void SetNextLevel()
        {
            CurrentLevelIndex++;
            SetLevel(CurrentLevelIndex);
        }
        
        private void UpdateLevelIndex()
        {
            //Save the level index
            GameStateManager.UpdateSaveData(this);
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


        public override void Cleanup()
        {
            ClearCurrentLevel();
        }
        #endregion
    }
}