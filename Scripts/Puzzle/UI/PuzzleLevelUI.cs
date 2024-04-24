using System;
using System.Collections;
using Kuantech.Core;
using Kuantech.Core.HyperCasual.UI;
using Kuantech.UI;
using TMPro;
using UnityEngine;

namespace Kuantech.Puzzle.UI
{
    public class PuzzleLevelUI : UICanvas
    {
        [Header("Widgets")]
        [SerializeField] private LevelIndicator LevelIndicator;
        // [SerializeField] private TMP_Text LevelIndexText;
        // [SerializeField] private string LevelLabel = "Level";

        [Header("Panels")]
        public PuzzleCompletePanel CompletePanel;
        public PuzzleFailPanel FailedPanel;
        public float CompletePanelShowDelay = 0f;
        public float FailedPanelShowDelay = 0f;

        [NonSerialized] public PuzzleLevel CurrentLevel;

        protected override void Start()
        {
            base.Start();
            Initialize();
        }
        public virtual void Initialize()
        {
            if(CompletePanel != null) CompletePanel.Initialize(this);
            if(FailedPanel != null) FailedPanel.Initialize(this);
            Reset();
        }

        public virtual void OnLevelSetup(PuzzleLevel level)
        {
            CurrentLevel = level;
            if(LevelIndicator != null) LevelIndicator.SetLevelIndex(level.LevelIndex + 1);
            level.OnStateChange += OnLevelStateChange;
        }

        public void OnLevelStateChange(LevelChangeData levelChangeData)
        {
            if(levelChangeData.NewState == LevelState.Completed)
            {
                OpenCompletePanel();
            }
            else if(levelChangeData.NewState == LevelState.Failed)
            {
                OpenFailedPanel();
            }else{
                Reset();
            }
        }

        public void ToggleLevelIndicator(bool toggle)
        {
            LevelIndicator.gameObject.SetActive(toggle);
        }
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
        public virtual void Reset()
        {
            if(CompletePanel != null) CompletePanel.Close();
            if(FailedPanel != null) FailedPanel.Close();
        }
        
        #region Boosters

        public virtual void SetUIForBooster(PuzzleBooster booster)
        {
            
        }

        public virtual void DisableBoosterUI()
        {
            
        }
        #endregion
    }
}