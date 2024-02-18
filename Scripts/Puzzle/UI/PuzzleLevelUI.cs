using System;
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

        [NonSerialized] public PuzzleLevel CurrentLevel;

        protected override void Start()
        {
            base.Start();
            Initialize();
        }
        public void Initialize()
        {
            CompletePanel.Initialize(this);
            FailedPanel.Initialize(this);
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
            CompletePanel.Show();
        }

        public void OpenFailedPanel()
        {
            FailedPanel.Show();
        }

        public virtual void Reset()
        {
            CompletePanel.Close();
            FailedPanel.Close();
        }
    }
}