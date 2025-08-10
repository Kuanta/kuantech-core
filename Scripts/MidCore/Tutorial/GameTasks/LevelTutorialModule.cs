using System;
using System.Collections.Generic;
using Kuantech.Utils;

namespace Kuantech.Core.MidCore
{
    
    
    public class LevelTutorialModule : LevelModule
    {
        public GameTaskManager TaskManager;
   
        public void SetTasks(int tasksIndex)
        {
            TaskManager.SetTasks(tasksIndex);
        }
        
        public override void OnLevelStateChange(LevelStateChangeData levelStateChangeData)
        {
            if (levelStateChangeData.NewState == LevelState.Playing)
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