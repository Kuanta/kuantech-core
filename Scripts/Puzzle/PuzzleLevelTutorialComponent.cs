using Kuantech.Core;
using UnityEngine;

namespace Kuantech.Puzzle
{
    /// <summary>
    /// This is a level component that implements a puzzle level tutorial
    /// </summary>
    [RequireComponent(typeof(GameTaskManager))]
    public class PuzzleLevelTutorialComponent : PuzzleLevelElement
    {
        public GameTaskManager taskManager;
        
        public override void OnSetupLevel()
        {
            taskManager.SetupTasks();
        }

        public override void OnPostPlayLevel()
        {
            //Start the tasks on post play so that all necessary level setups are made
            taskManager.StartTasks();
        }

        public override void Reset()
        {
            taskManager.Restart();
        }
    }
}