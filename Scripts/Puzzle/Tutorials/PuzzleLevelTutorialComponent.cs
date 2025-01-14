using System;
using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Puzzle
{
    [Serializable]
    public struct TutorialState
    {
        public int CurrentTaskIndex;
    }
    
    [Serializable]
    public struct TasksCollection
    {
        public List<GameTask> Tasks;
    }
    
    /// <summary>
    /// This is a level component that implements a puzzle level tutorial
    /// </summary>
    [RequireComponent(typeof(GameTaskManager))]
    public class PuzzleLevelTutorialComponent : PuzzleLevelElement
    {
        public GameTaskManager taskManager;
        public int TaskToStart = 0;
        public List<TasksCollection> Tasks;
        
        public override void OnSetupLevel()
        {
            if(taskManager.Tasks != null) taskManager.Tasks.Clear();
            int tutorialIndex = (ParentLevel as PuzzleLevel).TutorialIndex;
            if (tutorialIndex < 0 || tutorialIndex >= Tasks.Count) return;
            taskManager.Tasks = Tasks[tutorialIndex].Tasks;
            foreach (var gameTask in taskManager.Tasks)
            {
                gameTask.ParentTaskManager = taskManager;
            }
            taskManager.SetupTasks();
        }
        
        public override void OnPostPlayLevel()
        {
            //Start the tasks on post play so that all necessary level setups are made
            taskManager.StartTasks(TaskToStart);
        }

        public override void Reset()
        {
            taskManager.Restart();
            TaskToStart = 0;
        }
        
         
        #region State
        public override void LoadElementState(byte[] state)
        {
            if (taskManager == null) return;
            if (state != null)
            {
                TutorialState tutorialState = Helpers.Deserialize<TutorialState>(state);
                TaskToStart = tutorialState.CurrentTaskIndex;
            }
            else
            {
                TaskToStart = 0;
            }
        }

        public override byte[] GetElementState()
        {
            return Helpers.Serialize(new TutorialState()
            {
                CurrentTaskIndex = taskManager.CurrentTaskIndex,
            });
        }
        #endregion
    }
}