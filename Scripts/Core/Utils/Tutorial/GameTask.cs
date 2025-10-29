using System;
using UnityEngine;

namespace Kuantech.Core
{
    [Serializable]
    
    public class GameTask : MonoBehaviour
    {
        [Header("Task Settings")]
        public GameTask NextTask;
        public string TaskId;
        public string TaskName;
        public GameTaskManager ParentTaskManager;
        public bool Completed = false;

        public virtual void SetupTask()
        {
            
        }
        public virtual void StartTask()
        {
            Completed = false;
        }

        public virtual void UpdateTask(float deltaTime)
        {
            
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

