using Kuantech.Puzzle.UI;
using UnityEngine;

namespace Kuantech.Core.MidCore
{
    public abstract class DragAndDropTask : LevelTutorialTask
    {
        public Vector3 DragStart;
        public Vector3 DragEnd;

        protected TutorialHand TutorialHand;
        public override void StartTask()
        {
            DragStart = GetStartPosition();
            DragEnd = GetEndPosition();
            TutorialHand = TutorialPanel != null ? TutorialPanel.GetTutorialHand() : null;
            if (TutorialHand != null)
            {
                TutorialHand.gameObject.SetActive(true);
                TutorialHand.DoSwipeMotionWorldToWorld(DragStart, DragEnd);
            }
            base.StartTask();
        }
        
        public override void UpdateTask(float deltaTime)
        {
            base.UpdateTask(deltaTime);
            if(TutorialHand == null) return;
            TutorialHand.SetStartSwipePositionFromWorldPosition(GetStartPosition());
            TutorialHand.SetEndSwipePositionFromWorldPosition(GetEndPosition());
        }
        
        public abstract Vector3 GetStartPosition();

        public abstract Vector3 GetEndPosition();

        public override void EndTask()
        {
            base.EndTask();
            if (TutorialHand == null) return;
            TutorialHand.gameObject.SetActive(false);
        }
    }
}