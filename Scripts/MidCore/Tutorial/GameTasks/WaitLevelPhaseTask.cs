namespace Kuantech.Core.MidCore
{
    public class WaitLevelPhaseTask : LevelTutorialTask
    {
        public string PhaseToWait;

        public override void StartTask()
        {
            base.StartTask();
            CheckPhase();
        }

        public override void UpdateTask(float deltaTime)
        {
            base.UpdateTask(deltaTime);
            if (CheckPhase()) return;
        }
        
        private bool CheckPhase()
        {
            LevelPhase phase = ParentLevel.GetCurrentPhase();
            if (phase == null) return false;
            if (phase.Key == PhaseToWait)
            {
                CompleteTask();
            }

            return true;
        }
    }
}