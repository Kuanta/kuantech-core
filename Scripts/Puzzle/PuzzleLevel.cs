using Kuantech.Core;
using Kuantech.Core.Utils;
using Kuantech.Puzzle.UI;
using UnityEngine;

namespace Kuantech.Puzzle
{
    public class PuzzleLevel : Level
    {
        public PuzzleLevelUI LevelUI;
        public ScreenSizeAdjuster ScreenSizeAdjuster;

        public override void SetupLevel()
        {
            LevelUI = PuzzleUIManager.GetLevelUI(); 
            if(LevelUI != null) LevelUI.OnLevelSetup(this);
            ResetUI();
            if(ScreenSizeAdjuster != null)
            {
                ScreenSizeAdjuster.FitCameraToAnchors();
            }
            base.SetupLevel();
        }

        public override void ResetLevelState()
        {
            base.ResetLevelState();
            ResetUI();
        }
        
        public virtual void EarnScore(int score)
        {

        }
        
        /// <summary>
        /// All the resetting about UI should be done here
        /// </summary>
        protected virtual void ResetUI()
        {

        }
    }
}