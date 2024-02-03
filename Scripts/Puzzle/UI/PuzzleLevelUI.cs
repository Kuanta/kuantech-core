using System;
using Kuantech.Core;
using Kuantech.UI;
using UnityEngine;

namespace Kuantech.Puzzle.UI
{
    public class PuzzleLevelUI : UICanvas
    {
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
            CompletePanel.Close();
            FailedPanel.Close();
        }

        public virtual void OnLevelSetup(PuzzleLevel level)
        {
            CurrentLevel = level;
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
                CompletePanel.Close();
                FailedPanel.Close();
            }
        }

        public void OpenCompletePanel()
        {
            CompletePanel.Show();
        }

        public void OpenFailedPanel()
        {
            FailedPanel.Show();
        }

    }
}