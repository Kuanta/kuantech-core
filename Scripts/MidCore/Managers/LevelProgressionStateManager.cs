using System;
using Kuantech.Core;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.Midcore
{
    [Serializable]
    public struct LevelIndexData
    {
        public int WorldIndex;
        public int LevelIndex;
    }
    
    /// <summary>
    /// Saves the progress for levels
    /// </summary>
    public class LevelProgressionStateManager : SubManager
    {
        [SaveableField] public LevelIndexData LastCompletedLevelNumberData;
        [SaveableField] public LevelIndexData CurrentLevelNumberData;

        public UnityAction<LevelIndexData> CurrentLevelChanged;
        public UnityAction<LevelIndexData> LastCompletedLevelChanged;
        
        public override void SetDefaultState()
        {
            base.SetDefaultState();
            SetCurrentLevel(0,0, false);
            SetLastCompletedLevel(-1,-1, false);
        }
        
        [Button("Set Current Level")]
        public void SetCurrentLevel(int worldIndex, int levelIndex, bool saveState = true)
        {
            CurrentLevelNumberData = new LevelIndexData()
            {
                LevelIndex = levelIndex,
                WorldIndex = worldIndex,
            };
            if(saveState) SaveState();
            CurrentLevelChanged?.Invoke(CurrentLevelNumberData);
        }

        [Button("Set Next Level")]
        public void SetNextLevel(bool saveState = true)
        {
            LevelManager lm = LevelManager.GetContext<LevelManager>();
            if (lm == null) return;
            LevelIndexData next = lm.GetCorrectedLevelIndex(new LevelIndexData()
            {
                WorldIndex = CurrentLevelNumberData.WorldIndex,
                LevelIndex = CurrentLevelNumberData.LevelIndex + 1,
            });
            SetCurrentLevel(next.WorldIndex, next.LevelIndex, saveState);
        }

        [Button("Set Prev Level")]
        public void SetPreviousLevel(bool saveState = true)
        {
            LevelManager lm = LevelManager.GetContext<LevelManager>();
            if (lm == null) return;
            int worldIndex = CurrentLevelNumberData.WorldIndex;
            int levelIndex = CurrentLevelNumberData.LevelIndex - 1;
            if (levelIndex < 0)
            {
                worldIndex = Mathf.Max(0, worldIndex - 1);
                WorldDataAsset world = lm.GetWorld(worldIndex);
                levelIndex = world != null ? Mathf.Max(0, world.Levels.Count - 1) : 0;
            }
            SetCurrentLevel(worldIndex, levelIndex, saveState);
        }

        [Button("Set Last Completed Level")]
        public void SetLastCompletedLevel(int worldIndex, int levelIndex, bool saveState = true)
        {
            LastCompletedLevelNumberData = new LevelIndexData()
            {
                LevelIndex = levelIndex,
                WorldIndex = worldIndex,
            };
            if(saveState) SaveState();
            LastCompletedLevelChanged?.Invoke(LastCompletedLevelNumberData);
        }
        
        public static void OnLevelCompleted(Level level)
        {
            var ctx = GetContext<LevelProgressionStateManager>();
            if (ctx == null) return;
            
            //naming is bad here
            int levelNumber = level.LevelNumber;
            int worldIndex = level.WorldNumber;
            
            //Last completed
            if (ctx.LastCompletedLevelNumberData.WorldIndex <= worldIndex)
            {
                ctx.SetLastCompletedLevel(worldIndex, levelNumber);
            }

            levelNumber += 1;
            ctx.CurrentLevelNumberData = new LevelIndexData()
            {
                WorldIndex = worldIndex,
                LevelIndex = levelNumber,
            };
            
            //Set next level
            LevelManager lm = LevelManager.GetContext<LevelManager>();
            if (lm != null)
            {
                LevelIndexData correctedData = lm.GetCorrectedLevelIndex(ctx.CurrentLevelNumberData);
                ctx.SetCurrentLevel(correctedData.WorldIndex, correctedData.LevelIndex);
            }
        }

        public static  LevelIndexData GetLevelProgressionData()
        {
            var ctx = GetContext<LevelProgressionStateManager>();
            if (ctx == null) return new  LevelIndexData()
            {
                LevelIndex = 0,
                WorldIndex = 0,
            };

            return new  LevelIndexData()
            {
                WorldIndex = ctx.CurrentLevelNumberData.WorldIndex,
                LevelIndex = ctx.CurrentLevelNumberData.LevelIndex,
            };
        }
        
        public static LevelIndexData GetLastCompletedLevelProgressionData()
        {
            var ctx = GetContext<LevelProgressionStateManager>();
            if (ctx == null) return new  LevelIndexData()
            {
                LevelIndex = -1,
                WorldIndex = -1,
            };

            return new  LevelIndexData()
            {
                WorldIndex = ctx.LastCompletedLevelNumberData.WorldIndex,
                LevelIndex = ctx.LastCompletedLevelNumberData.LevelIndex,
            };
        }
    }
}