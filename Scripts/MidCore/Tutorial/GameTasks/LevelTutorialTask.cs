using System;
using Kuantech.Core.UI;
using Kuantech.Midcore.UI;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Core.MidCore
{
    public class LevelTutorialTask : GameTask
    {
        [Header("Tutorial Text")] 
        public string TutorialText;
        public bool HideTutorialTextOnComplete = true;
        
        //Runtime
        [NonSerialized] public Level ParentLevel;
        [NonSerialized] public TutorialPanel TutorialPanel;
        
        public override void SetupTask()
        {
            ParentLevel = LevelManager.GetCurrentLevel();
            if (ParentLevel == null) return;
            LevelUI levelUI = UIManager.GetLevelUI();
            if (levelUI == null) return;
            TutorialPanel = levelUI.GetUIElementByType<TutorialPanel>();
        }

        public override void StartTask()
        {
            base.StartTask();
            SetTutorialText();
        }
        
        protected virtual void SetTutorialText()
        {
            if (TutorialPanel == null) return;
            if (TutorialText.IsNullOrEmpty()) return;
            TutorialPanel.SetTutorialText(TutorialText);
            TutorialPanel.ToggleTutorialText(true);
        }
        public override void EndTask()
        {
            base.EndTask();
            if(TutorialPanel == null) return;
            if(HideTutorialTextOnComplete) TutorialPanel.ToggleTutorialText(false);
        }
    }
}