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
            ParentPuzzleLevel.PuzzleLevelUI.ToggleTutorialHand(true);
            DragStart = GetStartPosition();
            DragEnd = GetEndPosition();
            ParentPuzzleLevel.PuzzleLevelUI.TutorialHand.DoSwipeMotionWorldToWorld(DragStart, DragEnd);
            base.StartTask();
        }

        public abstract Vector3 GetStartPosition();

        public abstract Vector3 GetEndPosition();
    }
}