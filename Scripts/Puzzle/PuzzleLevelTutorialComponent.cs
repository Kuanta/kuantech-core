using System;
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
    
    /// <summary>
    /// This is a level component that implements a puzzle level tutorial
    /// </summary>
    [RequireComponent(typeof(GameTaskManager))]
    public class PuzzleLevelTutorialComponent : PuzzleLevelElement
    {
        public GameTaskManager taskManager;
        public int TaskToStart = 0;
        public override void OnSetupLevel()
        {
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