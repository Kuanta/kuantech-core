using System.Collections;
using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.Core.UI;
using UnityEngine;

namespace Kuantech.Midcore.UI
{
    public class MidcoreLevelUI : LevelUI
    {
        [Header("Panels")] 
        [SerializeField] private MidcoreCompletePanel CompletePanel;
        [SerializeField] private MidcoreFailPanel FailPanel;
        [SerializeField] private float CompletePanelShowDelay = 0f;
        [SerializeField] private float FailedPanelShowDelay = 0f;
        
        public override void Initialize()
        {
            base.Initialize();
            if (FailPanel!= null)
            {
                FailPanel.Initialize();
            }

            if (CompletePanel != null)
            {
                CompletePanel.Initialize();
            }
        }
        
        public override void OnLevelSetup(Level level)
        {
            base.OnLevelSetup(level);
            level.OnStateChangeEvent += OnLevelStateChange;
        }
        
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
            FailPanel.Open();
        }
        
        public override void Reset()
        {
            base.Reset();
            if(CompletePanel != null) CompletePanel.Close();
            if(FailPanel != null) FailPanel.Close();
        }
    }
}