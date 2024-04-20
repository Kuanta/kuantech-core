using System.Collections.Generic;
using System.Linq;
using Kuantech.Core;
using Kuantech.Core.Utils;
using Kuantech.Puzzle.UI;

namespace Kuantech.Puzzle
{
    public class PuzzleLevel : Level
    {
        public PuzzleLevelUI LevelUI;
        public ScreenSizeAdjuster ScreenSizeAdjuster;
        public List<PuzzleLevelElement> LevelElements = new List<PuzzleLevelElement>();

        public override void SetupLevel()
        {
            LevelUI = PuzzleUIManager.GetLevelUI(); 
            if(LevelUI != null) LevelUI.OnLevelSetup(this);
            ResetUI();
            if(ScreenSizeAdjuster != null)
            {
                ScreenSizeAdjuster.FitCameraToAnchors();
            }
            LevelElements = GetComponentsInChildren<PuzzleLevelElement>().ToList();
            foreach(var element in LevelElements)
            {
                element.OnSetup();
            }
            base.SetupLevel();
        }

        public override void PlayLevel()
        {
            base.PlayLevel();
            foreach (var element in LevelElements)
            {
                element.OnPlay();
            }
        }
        public override void ResetLevelState()
        {
            base.ResetLevelState();
            foreach (var element in LevelElements)
            {
                element.OnRestart();
            }
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