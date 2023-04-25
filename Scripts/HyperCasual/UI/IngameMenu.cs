using Kuantech.UI;

namespace Kuantech.Core.HyperCasual
{
    public class IngameMenu : UIMenu
    {
        public LevelCompletePanel LevelCompletePanel;
        public LevelFailedPanel LevelFailedPanel;
        
        public override void Show()
        {
            base.Show();
            LevelCompletePanel.Close();
            LevelFailedPanel.Close();
        }
        
        public override void Close()
        {
            base.Close();
        }

        public virtual void SetEarnings(int earnedCoins)
        {
            LevelCompletePanel.SetEarnings(earnedCoins);
        }
        
        public void OnStateChange(LevelState newState)
        {
            if (newState == LevelState.Completed)
            {
                LevelCompletePanel.Show();
            }
            else
            {
                LevelCompletePanel.Close();
            }

            if (newState == LevelState.Failed)
            {
                LevelFailedPanel.Show();
            }
            else
            {
                LevelFailedPanel.Close();
            }
        }
    }
}