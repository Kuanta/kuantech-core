using Kuantech.Core.MidCore;

namespace Kuantech.Midcore.Tutorial.Common
{
    public class MarkAsCompletedTask : TutorialTask
    {
        public override void StartTask()
        {
            base.StartTask();
            TutorialManager.MarkTutorialAsCompleted(ParentTutorial.TutorialId);
            CompleteTask();
        }
    }
}