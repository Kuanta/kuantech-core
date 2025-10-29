using Kuantech.Core.UI;
using Kuantech.Midcore.UI;

namespace Kuantech.Core.MidCore
{
    public class ToggleTutorialPanelTask : LevelTutorialTask
    {
        public bool Toggle;
        public override void StartTask()
        {
            base.StartTask();
            CompleteTask();
            LevelUI levelUI = UIManager.GetLevelUI();
            if (levelUI == null) return;
            var tutorialPanel = levelUI.GetUIElementByType<TutorialPanel>();
            if (tutorialPanel == null) return;
            
            if (Toggle)
            {
                tutorialPanel.Show();
            }
            else
            {
                tutorialPanel.Close();
            }
        }
        
    }
}