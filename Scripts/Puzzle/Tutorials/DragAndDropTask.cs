using UnityEngine;

namespace Kuantech.Puzzle.Tutorials
{
    /// <summary>
    /// For tutorial tasks where players must drag and drop draggables
    /// </summary>
    public abstract class DragAndDropTask : PuzzleTutorialTask
    {
        public Vector3 DragStart;
        public Vector3 DragEnd;
        
        
        public override void StartTask()
        {
            ParentPuzzleLevel.LevelUI.ToggleTutorialHand(true);
            SetStartPosition();
            SetEndPosition();
            ParentPuzzleLevel.LevelUI.TutorialHand.DoSwipeMotionWorldToWorld(DragStart, DragEnd);
            base.StartTask();
        }

        public abstract void SetStartPosition();

        public abstract void SetEndPosition();
    }
}