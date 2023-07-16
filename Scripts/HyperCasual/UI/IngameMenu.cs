using Kuantech.UI;

namespace Kuantech.Core.HyperCasual
{
    public class IngameMenu : UIMenu
    {
        public LevelCompletePanel LevelCompletePanel;
        public LevelFailedPanel LevelFailedPanel;

        public void Initialize()
        {
            if(LevelCompletePanel != null) LevelCompletePanel.Initialize();
            if(LevelFailedPanel != null) LevelFailedPanel.Initialize();
        }
        
        public override void Show()
        {
            base.Show();
            if(LevelCompletePanel != null) LevelCompletePanel.Close();
            if(LevelFailedPanel != null) LevelFailedPanel.Close();
        }
        
        public override void Close()
        {
            base.Close();
        }
        
        public void OnStateChange(LevelState newState)
        {
            if (newState == LevelState.Completed && LevelCompletePanel != null)
            {
                LevelCompletePanel.Show();
            }
            else if(LevelCompletePanel != null)
            {
                LevelCompletePanel.Close();
            }

            if (newState == LevelState.Failed && LevelFailedPanel != null)
            {
                LevelFailedPanel.Show();
            }
            else if(LevelFailedPanel != null)
            {
                LevelFailedPanel.Close();
            }
        }
    }
}