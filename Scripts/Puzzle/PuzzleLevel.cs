using Kuantech.Core;
using Kuantech.Puzzle.UI;

namespace Kuantech.Puzzle
{
    public class PuzzleLevel : Level
    {
        public PuzzleLevelUI LevelUI;
        public override void SetupLevel()
        {
            LevelUI = PuzzleUIManager.GetLevelUI(); 
            if(LevelUI != null) LevelUI.OnLevelSetup(this);
            ResetUI();
            base.SetupLevel();
        }

        public override void ResetLevelState()
        {
            base.ResetLevelState();
            ResetUI();
        }
        
        /// <summary>
        /// All the resetting about UI should be done here
        /// </summary>
        protected virtual void ResetUI()
        {

        }
    }
}