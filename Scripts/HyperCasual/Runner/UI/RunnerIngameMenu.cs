using System.Collections;
using Kuantech.Core.HyperCasual;
using Kuantech.Core.HyperCasual.UI;
using Kuantech.HyperCasual.UI;
using Kuantech.UI;
using UnityEngine;

namespace Kuantech.HyperCasual.Runner.UI
{
    public class RunnerIngameMenu : UIMenu
    {
        public LevelCompletePanel LevelCompletePanel;
        public LevelFailedPanel LevelFailedPanel;

        [Header("Delay Timings")] 
        [SerializeField] private float LevelCompletePanelShowDelay;
        [SerializeField] private float LevelFailedPanelShowDelay;

        public virtual void Initialize()
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

        
        public void OnStateChange(LevelState newState)
        {
            if (newState == LevelState.Completed && LevelCompletePanel != null)
            {
                StartCoroutine(ShowPanelRoutine(LevelCompletePanel, LevelCompletePanelShowDelay));
            }
            else if(LevelCompletePanel != null)
            {
                LevelCompletePanel.Close();
            }

            if (newState == LevelState.Failed && LevelFailedPanel != null)
            {
                StartCoroutine(ShowPanelRoutine( LevelFailedPanel, LevelFailedPanelShowDelay));
            }
            else if(LevelFailedPanel != null)
            {
                LevelFailedPanel.Close();
            }
        }

        private IEnumerator ShowPanelRoutine(UIMenu panel, float delay)
        {
            yield return new WaitForSeconds(delay);
            panel.Show();
        }
    }
}