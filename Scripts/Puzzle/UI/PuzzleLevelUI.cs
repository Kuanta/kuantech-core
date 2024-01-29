using System;
using Kuantech.Core;
using Kuantech.UI;
using UnityEngine;

namespace Kuantech.Puzzle.UI
{
    public class PuzzleLevelUI : UIMenu
    {
        [Header("Panels")]
        public UIMenu CompletePanel;
        public UIMenu FailedPanel;
        [NonSerialized] protected PuzzleLevel CurrentLevel;
        public virtual void OnLevelSetup(PuzzleLevel level)
        {
            CurrentLevel = level;
            level.OnStateChange += OnLevelStateChange;
        }

        public void OnLevelStateChange(LevelChangeData levelChangeData)
        {
            
        }
    }
}