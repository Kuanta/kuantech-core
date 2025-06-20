using System.Collections;
using Kuantech.Core;
using Kuantech.Core.HyperCasual.UI;
using Kuantech.Core.UI;
using UnityEngine;
using UnityEngine.Serialization;

namespace Kuantech.HyperCasual.Runner.UI
{
    public class RunnerIngameMenu : UIMenu
    {
        [FormerlySerializedAs("midcoreCompletePanel")] [FormerlySerializedAs("LevelCompletePanel")] public CompletePanel completePanel;
        public LevelFailedPanel LevelFailedPanel;

        [Header("Delay Timings")] 
        [SerializeField] private float LevelCompletePanelShowDelay;
        [SerializeField] private float LevelFailedPanelShowDelay;

        public virtual void Initialize()
        {
            if(completePanel != null) completePanel.Initialize();
            if(LevelFailedPanel != null) LevelFailedPanel.Initialize();
        }
        
        public override void Open()
        {
            base.Open();
            if(completePanel != null) completePanel.Close();
            if(LevelFailedPanel != null) LevelFailedPanel.Close();
        }

        
        public void OnStateChange(LevelState newState)
        {
            if (newState == LevelState.Completed && completePanel != null)
            {
                StartCoroutine(ShowPanelRoutine(completePanel, LevelCompletePanelShowDelay));
            }
            else if(completePanel != null)
            {
                completePanel.Close();
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
            panel.Open();
        }
    }
}