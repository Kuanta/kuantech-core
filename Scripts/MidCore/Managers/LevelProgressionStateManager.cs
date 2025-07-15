using Kuantech.Core;

namespace Kuantech.Midcore
{
    /// <summary>
    /// Saves the progress for levels
    /// </summary>
    public class LevelProgressionStateManager : SubManager
    {
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
            if (ctx.LastCompletedWorld < level.WorldIndex)
            {
                ctx.LastCompletedWorld = level.WorldIndex;
            }

            if (ctx.LastCompletedLevelIndex < level.LevelNumber)
            {
                ctx.LastCompletedLevelIndex = level.LevelNumber;
            }
            ctx.SaveState();
        }
    }
}