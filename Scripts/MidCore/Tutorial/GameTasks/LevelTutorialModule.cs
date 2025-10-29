using System.Collections.Generic;
using Kuantech.Midcore.Tutorial;
using Kuantech.Utils;

namespace Kuantech.Core.MidCore
{
    public class LevelTutorialModule : LevelModule
    {
        public GameTaskManager TaskManager;

        public List<Tutorial> Tutorials;


        public void SetTutorial(int index)
        {
            TaskManager.Tasks = null;
            if (Tutorials.IsValidIndex(index))
            {
                Tutorials[index].SetTasks(TaskManager);
            }
        }

        public override void OnLevelStateChange(LevelStateChangeData levelStateChangeData)
        {
            if (levelStateChangeData.NewState == LevelState.Playing && !TaskManager.Tasks.IsNullOrEmpty())
            {
                TaskManager.StartTasks();
            }
        }
        
        public override void OnReset()
        {
            TaskManager.Restart();
        }
    }
}