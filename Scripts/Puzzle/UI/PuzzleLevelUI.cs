using System;
using System.Collections;
using Kuantech.Core;
using Kuantech.Core.HyperCasual.UI;
using Kuantech.Core.UI;
using Kuantech.HyperCasual.UI;
using Kuantech.UI;
using UnityEngine;

namespace Kuantech.Puzzle.UI
{
    public class PuzzleLevelUI : UICanvas
    {
        [Header("Widgets")]
        [SerializeField] private LevelIndicator LevelIndicator;
        // [SerializeField] private TMP_Text LevelIndexText;
        // [SerializeField] private string LevelLabel = "Level";

        [Header("Buttons")] 
        [SerializeField] private ConfirmPanelButton RestartButton;

        [SerializeField] private ConfirmPanel RestartConfirmPanel;
        
        [Header("Panels")] 
        public WinConditionIndicatorPanel WinConditionIndicatorPanel;
        public PuzzleCompletePanel CompletePanel;
        public PuzzleFailPanel FailedPanel;
        public BoostersHUD BoostersHUD;
        public float CompletePanelShowDelay = 0f;
        public float FailedPanelShowDelay = 0f;

        [Header("Tutorial")] 
        [SerializeField] public TutorialHand TutorialHand;
        [NonSerialized] public PuzzleLevel CurrentLevel;

        public virtual void Initialize()
        {
            if(CompletePanel != null) CompletePanel.Initialize(this);
            if(FailedPanel != null) FailedPanel.Initialize(this);

            if (RestartConfirmPanel != null)
            {
                RestartConfirmPanel.OnConfirm += () =>
                {
                    CurrentLevel.RestartLevel();
                };
            }

            if (BoostersHUD != null)
            {
                BoostersHUD.Initialize(this);
            }
        }
        
        public virtual void OnLevelSetup(PuzzleLevel level)
        {
            CurrentLevel = level;
            if(LevelIndicator != null) LevelIndicator.SetLevelIndex(level.LevelNumber + 1);
            level.OnStateChange += OnLevelStateChange;
            
            //Set win conditions
            WinConditionTracker tracker = CurrentLevel.WinConditionTracker;

            if (WinConditionIndicatorPanel != null && tracker != null)
            {
                WinConditionIndicatorPanel.SetTracker(tracker);
            }

            if (BoostersHUD != null)
            {
                BoostersHUD.OnLevelSetup(level);
            }
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

        public void ToggleLevelIndicator(bool toggle)
        {
            LevelIndicator.gameObject.SetActive(toggle);
        }

        #region Score Earning
        public void SetScore(string key, WinConditionTracker scoreTracker)
        {
            int currentAmount = scoreTracker.GetCollectedAmount(key);
            int targetAmount = scoreTracker.GetTargetAmount(key);
            int remaining = Mathf.Max(targetAmount - currentAmount, 0);
            if(WinConditionIndicatorPanel != null) WinConditionIndicatorPanel.SetScore(key, currentAmount, remaining);
        }
        #endregion
        
        #region Win Lose Panels
        public void OpenCompletePanel()
        {
            StartCoroutine(_OpenCompletePanel());
        }
        private IEnumerator _OpenCompletePanel()
        {
            yield return new WaitForSeconds(CompletePanelShowDelay);
            if(CompletePanel != null) CompletePanel.Show();
        }
        public void OpenFailedPanel()
        {
            StartCoroutine(_OpenFailedPanel());
        }
        private IEnumerator _OpenFailedPanel()
        {
            yield return new WaitForSeconds(FailedPanelShowDelay);
            FailedPanel.Show();
        }
        #endregion
        
        #region Score panels

        public void OnStageCompleted(int completedStageIndex)
        {
            WinConditionIndicatorPanel.OnStageCompleted();  
        }
        
        public void OnNewStage(int newStageIndex)
        {
            WinConditionIndicatorPanel.OnNewStage(newStageIndex);
        }
        #endregion
        
        public virtual void Reset()
        {
            if(CompletePanel != null) CompletePanel.Close();
            if(FailedPanel != null) FailedPanel.Close();
            if (WinConditionIndicatorPanel != null)
            {
                WinConditionIndicatorPanel.SetPanelForStage();  
            }
            if(TutorialHand != null) TutorialHand.gameObject.SetActive(false);
            if(BoostersHUD != null) BoostersHUD.Reset();
        }
        
        #region Boosters

        public virtual void SetUIForBooster(PuzzleBooster booster)
        {
            //Can hide additional UI elements here
        }

        public virtual void DisableBoosterUI()
        {
            if(BoostersHUD != null) BoostersHUD.OnBoosterDeactivated();
        }
        #endregion
    }
}