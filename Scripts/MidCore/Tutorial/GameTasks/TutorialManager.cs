using System;
using System.Collections.Generic;
using Kuantech.Midcore.Tutorial;
using Kuantech.Utils;
using Sirenix.OdinInspector;
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
        
        public List<Tutorial> Tutorials;
        
        [SaveableField]
        public List<string> CompletedTutorials;
        
        [SerializeField]
        private GameTaskManager GameTaskManager;
        
        
        [HideInInspector] 
        [SaveableField] 
        public List<CompletedTaskData> CompletedTasks;
        
        public override void OnSubmanagersInitialized()
        {
            base.OnSubmanagersInitialized();
            CheckTutorialToStart();
        }

        public void CheckTutorialToStart()
        {
            foreach (var tut in Tutorials)
            {
                if(IsTutorialCompleted(tut.TutorialId)) continue;
                if (tut.CanStartTask())
                {
                    StartTutorial(tut);
                    return;
                }
                else
                {
                    return; //Don't skip to other tutorials without completing the current one
                }
            }
        }
        
        public int GetCurrentTasksIndex()
        {
            if (CompletedTasks.IsNullOrEmpty()) return 0;
            return -1; //Temporary
        }
        
        public static bool IsTutorialCompleted(string tutorialId)
        {
            var tutorialManager = GetContext<TutorialManager>();
            if (tutorialManager == null) return false;
            return tutorialManager.CompletedTutorials.Contains(tutorialId);
        }
        
        public static void MarkTutorialAsCompleted(string tutorialId)
        {
            var tutorialManager = GetContext<TutorialManager>();
            if (tutorialManager == null) return;
            if (!tutorialManager.CompletedTutorials.Contains(tutorialId))
            {
                tutorialManager.CompletedTutorials.Add(tutorialId);
            }
            tutorialManager.SaveState();
        }
        
        [Button("Start Tutorial")]
        public static void StartTutorial(Tutorial tutorial)
        {
            var tutorialManager = GetContext<TutorialManager>();
            if (tutorialManager == null) return;
            tutorial.SetTasks(tutorialManager.GameTaskManager);
            tutorialManager.GameTaskManager.StartTasks();
        }
        
        
    }
}