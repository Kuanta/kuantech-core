using System.Collections;
using Kuantech.Core;
using Kuantech.Core.UI;
using Kuantech.Rpg;
using UnityEngine;

namespace Kuantech.TowerDefense
{
    public class TowerDefenseLevelUI : LevelUI
    {
        [Header("UI Elements")]
        [SerializeField] private Healthbar Healthbar;

        [Header("Panels")] 
        public LevelCompletePanel CompletePanel;
        public LevelFailPanel FailedPanel;
        public float CompletePanelShowDelay = 0f;
        public float FailedPanelShowDelay = 0f;
        
        public override void Initialize()
        {
            base.Initialize();
            if (FailedPanel!= null)
            {
                FailedPanel.Initialize();
            }

            if (CompletePanel != null)
            {
                CompletePanel.Initialize();
            }
        }

        public override void OnLevelSetup(Level level)
        {
            base.OnLevelSetup(level);
            level.OnStateChange += OnLevelStateChange;
        }
        
        #region Win Lose Panels
        private void OnLevelStateChange(LevelStateChangeData levelStateChangeData)
        {
            if(levelStateChangeData.NewState == LevelState.Completed)
            {
                OpenCompletePanel();
            }
            else if(levelStateChangeData.NewState == LevelState.Failed)
            {
                OpenFailedPanel();
            }else if(levelStateChangeData.NewState == LevelState.Playing){ //todo:Resetting at play may cause issues
                Reset();
            }
        }
        public void OpenCompletePanel()
        {
            StartCoroutine(_OpenCompletePanel());
        }
        private IEnumerator _OpenCompletePanel()
        {
            yield return new WaitForSeconds(CompletePanelShowDelay);
            if(CompletePanel != null) CompletePanel.Open();
        }
        public void OpenFailedPanel()
        {
            StartCoroutine(_OpenFailedPanel());
        }
        private IEnumerator _OpenFailedPanel()
        {
            yield return new WaitForSeconds(FailedPanelShowDelay);
            FailedPanel.Open();
        }
        #endregion
        
        #region Health
        public void SetHealthText(float health, float maxHealth)
        {
            Healthbar.SetHealth(health, maxHealth);
        }
        #endregion

        public override void Reset()
        {
            if(CompletePanel != null) CompletePanel.Close();
            if(FailedPanel != null) FailedPanel.Close();
        }
    }
}