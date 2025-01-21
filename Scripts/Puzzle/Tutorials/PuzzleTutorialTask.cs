using Kuantech.Core;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Puzzle.Tutorials
{
    /// <summary>
    /// Base puzzle tutorial task for simply showing a text
    /// </summary>
    public class PuzzleTutorialTask : GameTask
    {
        [Header("Tutorial Text")] public string TutorialText;

        protected PuzzleLevel ParentPuzzleLevel;

        public override void SetupTask()
        {
            ParentPuzzleLevel = LevelManager.GetCurrentLevel() as PuzzleLevel;
            base.SetupTask();
        }
        public override void StartTask()
        {
            base.StartTask();
            if (ParentPuzzleLevel == null) return;
            if (TutorialText.IsNullOrEmpty()) return;
            ParentPuzzleLevel.LevelUI.SetTutorialText(TutorialText);
            ParentPuzzleLevel.LevelUI.ToggleTutorialText(true);
        }

        public override void EndTask()
        {
            if (ParentPuzzleLevel == null) return;
            ParentPuzzleLevel.LevelUI.ToggleTutorialText(false);
            ParentPuzzleLevel.LevelUI.ToggleTutorialHand(false);
            base.EndTask();
        }
    }
}