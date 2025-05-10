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

        [Header("Buttons")] 
        [SerializeField] private ConfirmPanelButton RestartButton;

        [SerializeField] private ConfirmPanel RestartConfirmPanel;

        [Header("Panels")] 
        public GameObject HUD;
        public WinConditionIndicatorPanel WinConditionIndicatorPanel;
        public PuzzleCompletePanel CompletePanel;
        public PuzzleFailPanel FailedPanel;
        public BoostersHUD BoostersHUD;
        public float CompletePanelShowDelay = 0f;
        public float FailedPanelShowDelay = 0f;
        public ComboIndicator ComboIndicator;
        
        [Header("Intro Panels")]
        public LevelIntroPanel HardLevelIntroPanel;
        public LevelIntroPanel BonusLevelIntroPanel;
        
        [Header("Tutorial")] 
        public TutorialHand TutorialHand;
        public TextBox TutorialTextBox;
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

            if (TutorialHand != null)
            {
                TutorialHand.ParentCanvas = this;
            }
        }
        
        public virtual void OnLevelSetup(PuzzleLevel level)
        {
            CurrentLevel = level;
            if (LevelIndicator != null)
            {
                LevelIndicator.SetLevelIndex(level.LevelNumber + 1);
            }
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

            if (HardLevelIntroPanel != null)
            {
                if (level.IsHardLevel() && HardLevelIntroPanel != null)
                {
                    HardLevelIntroPanel.gameObject.SetActive(true);
                    HardLevelIntroPanel.PlayAnimation();
                }else if (HardLevelIntroPanel != null && !level.IsHardLevel())
                {
                    HardLevelIntroPanel.gameObject.SetActive(false);
                }
            }
            
            if (BonusLevelIntroPanel != null)
            {
                if (level.IsBonusLevel() && BonusLevelIntroPanel != null)
                {
                    BonusLevelIntroPanel.gameObject.SetActive(true);
                    BonusLevelIntroPanel.PlayAnimation();
                }
                else
                {
                    BonusLevelIntroPanel.gameObject.SetActive(false);
                }
            }

            if (LevelIndicator != null)
            {
                LevelIndicator.SetStageCount(tracker?.GetStageCount() ?? 1);
                LevelIndicator.SetHardLevel(level.IsHardLevel());
                LevelIndicator.SetBonusLevel(level.IsBonusLevel());
            }
        }

        public virtual void OnPlayLevel()
        {
            if (LevelIndicator != null)
            {
                LevelIndicator.SetCurrentStage(CurrentLevel.GetCurrentStage());
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

        public void TriggerCombo(int comboIndex)
        {
            if (ComboIndicator == null) return;
            ComboIndicator.TriggerCombo(comboIndex);
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
        
        #region Score panels

        public void OnStageCompleted(int completedStageIndex)
        {
            if(WinConditionIndicatorPanel != null) WinConditionIndicatorPanel.OnStageCompleted();  
        }
        
        public void OnNewStage(int newStageIndex)
        {
            if(WinConditionIndicatorPanel != null) WinConditionIndicatorPanel.OnNewStage(newStageIndex);
            if (LevelIndicator != null)
            {
                LevelIndicator.SetCurrentStage(newStageIndex);
            }
        }
        #endregion

        #region Tutorial
        
        /// <summary>
        /// Sets the tutorial text
        /// </summary>
        /// <param name="tutorialText"></param>
        public void SetTutorialText(string tutorialText)
        {
            if (TutorialTextBox == null) return;
            TutorialTextBox.SetText(tutorialText);
        }

        public void ToggleTutorialText(bool toggle)
        {
            if (TutorialTextBox == null) return;
            TutorialTextBox.gameObject.SetActive(toggle);
        }

        public void ToggleTutorialHand(bool toggle)
        {
            if (TutorialHand == null) return;
            TutorialHand.gameObject.SetActive(toggle);
        }
        #endregion

        public virtual void ToggleHUD(bool toggle)
        {
            if (HUD != null)
            {
                HUD.SetActive(toggle);
            }
        }

        public override void Show()
        {
            ToggleHUD(true);
        }

        public override void Close()
        {
            ToggleHUD(false);
        }
        
        public virtual void Reset()
        {
            if(CompletePanel != null) CompletePanel.Close();
            if(FailedPanel != null) FailedPanel.Close();
            if (WinConditionIndicatorPanel != null)
            {
                WinConditionIndicatorPanel.SetPanelForStage();  
            }
            ToggleTutorialHand(false);
            ToggleTutorialText(false);
            if (ComboIndicator != null)
            {
                ComboIndicator.Reset();
            }
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