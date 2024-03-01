using System;
using UnityEngine;

namespace Kuantech.Core
{
    [Serializable]
    
    public class GameTutorialTask : MonoBehaviour
    {
        public GameTutorialTask NextTask;
        public string TaskId;
        public string TaskName;
        public GameTutorialTaskManager ParentTaskManager;
        public bool Completed = false;

        public virtual void StartTask()
        {
            Completed = false;
        }

        public virtual void CompleteTask()
        {
            if(Completed) return;
            Completed = true;
            EndTask();
            ParentTaskManager.OnTaskCompleted();
        }

        public virtual void EndTask()
        {
            
        }
    }
}

