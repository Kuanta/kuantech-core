using System;
using System.Collections.Generic;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Core.MidCore
{
    public class TutorialManager : SubManager
    {
        [Serializable]
        public struct CompletedTaskData
        {
            public int TaskIndex;
        }
        
        [SerializeField]
        private GameTaskManager GameTaskManager;
        
        [SaveableField]
        public List<CompletedTaskData> CompletedTasks;
        
        public override void OnSubmanagersInitialized()
        {
            base.OnSubmanagersInitialized();
            if (GameTaskManager == null) return;
            GameTaskManager.SetTasks(GetCurrentTasksIndex());
            if (!GameTaskManager.Tasks.IsNullOrEmpty())
            {
                GameTaskManager.StartTasks();
            }
        }

        public int GetCurrentTasksIndex()
        {
            if (CompletedTasks.IsNullOrEmpty()) return 0;
            return -1; //Temporary
        }
        
    }
}