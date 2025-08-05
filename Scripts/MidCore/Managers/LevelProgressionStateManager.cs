using System;
using Kuantech.Core;
using Sirenix.OdinInspector;
using UnityEngine.Events;
using UnityEngine.Serialization;

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
                int lastCompletedWorldIndex = worldIndex;
                int lastCompleteLevelIndex = ctx.LastCompletedLevelNumberData.LevelIndex;
                if (ctx.LastCompletedLevelNumberData.LevelIndex < levelNumber)
                {
                    lastCompletedWorldIndex = levelNumber;
                }
                ctx.SetLastCompletedLevel(lastCompletedWorldIndex, lastCompleteLevelIndex);
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