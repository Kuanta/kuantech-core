using System;
using System.Collections.Generic;
using System.Linq;
using Kuantech.Core;
using UnityEngine;

namespace Kuantech.Midcore.Tutorial
{
    public class Tutorial : MonoBehaviour
    {
        public string TutorialId;
        [NonSerialized] public List<GameTask> TutorialTasks;

        public void SetTasks(GameTaskManager taskManager)
        {
            TutorialTasks = GetComponentsInChildren<GameTask>().ToList();
            taskManager.SetTasks(TutorialTasks);
        }

        public virtual bool CanStartTask()
        {
            return true;
        }
    }
}