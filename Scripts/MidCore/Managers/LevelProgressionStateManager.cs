using Kuantech.Core;

namespace Kuantech.Midcore
{
    /// <summary>
    /// Saves the progress for levels
    /// </summary>
    public class LevelProgressionStateManager : SubManager
    {
        public struct LevelProgressionData
        {
            public int LastCompletedWorld;
            public int LastCompletedLevel;
        }
        
        [SaveableField] public int LastCompletedWorld;
        [SaveableField] public int LastCompletedLevelIndex;
        
        public override void SetDefaultState()
        {
            base.SetDefaultState();
            LastCompletedWorld = -1;
            LastCompletedLevelIndex = -1;
        }
        
        public static void OnLevelCompleted(Level level)
        {
            var ctx = GetContext<LevelProgressionStateManager>();
            if (ctx == null) return;
            
            if (ctx.LastCompletedLevelIndex < level.LevelIndex )
            {
                ctx.LastCompletedLevelIndex = level.LevelIndex;
            }

            if (ctx.LastCompletedWorld < level.WorldIndex)
            {
                ctx.LastCompletedLevelIndex = level.LevelIndex;
            }
            
            if (ctx.LastCompletedWorld < level.WorldIndex)
            {
                ctx.LastCompletedWorld = level.WorldIndex;
            }

          
            ctx.SaveState();
        }

        public static LevelProgressionData GetLevelProgressionData()
        {
            var ctx = GetContext<LevelProgressionStateManager>();
            if (ctx == null) return new LevelProgressionData()
            {
                LastCompletedLevel = -1,
                LastCompletedWorld = -1,
            };

            return new LevelProgressionData()
            {
                LastCompletedWorld = ctx.LastCompletedWorld,
                LastCompletedLevel = ctx.LastCompletedLevelIndex,
            };
        }
    }
}