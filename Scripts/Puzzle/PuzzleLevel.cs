using System;
using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.Core.Utils;
using Kuantech.Puzzle.UI;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Puzzle
{
    public class PuzzleLevel : Level
    {
        [Header("UI")]
        public PuzzleLevelUI LevelUI;
        
        [Header("Screen Size Adjuster")]
        public ScreenSizeAdjuster ScreenSizeAdjuster;
        
        public Dictionary<int, PuzzleLevelElement> LevelElements = new Dictionary<int, PuzzleLevelElement>();
        [NonSerialized] public PuzzleLevelState CurrentPuzzleLevelState;

        [Header("Win Condition Tracker")] 
        public List<WinConditionTracker.LevelStageEntry> StageEntries;
        public WinConditionTracker WinConditionTracker;
        
        //Boosters
        [NonSerialized] public PuzzleBooster CurrentBooster;
        
        public override void SetupLevel()
        {
            CreateWinConditionTracker();
            LevelUI = PuzzleUIManager.GetLevelUI(); 
            if(LevelUI != null) LevelUI.OnLevelSetup(this);
            ResetUI();
            
            if(ScreenSizeAdjuster != null)
            {
                ScreenSizeAdjuster.FitCameraToAnchors();
            }
            LevelElements = new Dictionary<int, PuzzleLevelElement>();
            PuzzleLevelElement[] levelElements = GetComponentsInChildren<PuzzleLevelElement>();
            for (int i = 0; i < levelElements.Length; ++i)
            {
                PuzzleLevelElement element = levelElements[i];
                element.OnSetup(this);
                LevelElements[i] = element;
            }
       
            base.SetupLevel();
        }

        public override void PlayLevel()
        {
            base.PlayLevel();
            foreach (var element in LevelElements.Values)
            {
                element.OnPlay();
            }
        }

        public override void ResetLevelState()
        {
            base.ResetLevelState();
            foreach (var element in LevelElements.Values)
            {
                element.OnRestart();
            }
            if(WinConditionTracker != null) WinConditionTracker.Reset();
            ResetBoosters();
            ResetUI();
        }

        protected virtual void ResetBoosters()
        {
            CancelCurrentBooster();
        }
        
        public virtual void EarnScore(string scoreKey, int score)
        {
            if(WinConditionTracker != null) WinConditionTracker.AddCollectedAmount(scoreKey, score);
            //CheckWinCondition();
            //Update UI
            LevelUI.SetScore(scoreKey, WinConditionTracker);
        }
        
        /// <summary>
        /// All the resetting about UI should be done here
        /// </summary>
        protected virtual void ResetUI()
        {
            //LevelUI.Reset();
        }
        
        #region WinCondition Tracker

        public virtual void CreateWinConditionTracker()
        {
            if (!StageEntries.IsNullOrEmpty())
            {
                WinConditionTracker = new WinConditionTracker();
                WinConditionTracker.SetStages(StageEntries);
            }

            WinConditionTracker.OnStageCompleted += OnStageCompleted;
            WinConditionTracker.OnAllStagesCompleted += OnStagesCompleted;
        }

        public void OnStageCompleted(int stageIndex)
        {
            LevelUI.OnStageCompleted(stageIndex);
            Debug.LogError("Stage Completed");
        }

        public void OnStagesCompleted()
        {
            CompleteLevel();
        }
        #endregion
        
        #region Level State
        public virtual PuzzleLevelState GetLevelState()
        {
            PuzzleLevelState levelState = new PuzzleLevelState();
            levelState.LevelElementStates = GetLevelElementsState();
            return levelState;
        }

        public virtual bool LoadLevelState(PuzzleLevelState newState)
        {
            //Load element states
            foreach(var elementStatePair in newState.LevelElementStates)
            {
                if(!LevelElements.ContainsKey(elementStatePair.Key)) continue;
                LevelElements[elementStatePair.Key].LoadElementState(elementStatePair.Value);
            }
            return true;
        }

        public Dictionary<int, byte[]> GetLevelElementsState()
        {
            Dictionary<int, byte[]> elementsState = new Dictionary<int, byte[]>();
            //Get elements state
            foreach(var pair in LevelElements)
            {
                PuzzleLevelElementState levelElementState = pair.Value.GetElementState();
                elementsState[pair.Key] = Helpers.Serialize(levelElementState);
            }
            return elementsState;
        }
        #endregion
        
        #region Boosters
        public virtual bool SetCurrentBooster(PuzzleBooster booster)
        {
            if (!booster.OnSetBooster(this)) return false;
            if (CurrentBooster != null)
            {
                CancelCurrentBooster();
            }

            if (!booster.OnSetBooster(this))
            {
                return false;
            }
            CurrentBooster = booster;
            LevelUI.SetUIForBooster(booster);
            return true;
        }
        
        public virtual void CancelCurrentBooster()
        {
            if(LevelUI != null) LevelUI.DisableBoosterUI();
            if (CurrentBooster == null) return;
            CurrentBooster.CancelBooster();
            CurrentBooster = null;
        }

        public bool CompleteBooster()
        {
            if (CurrentBooster == null) return false;
            bool result = CurrentBooster.CompleteBooster();
            if (!result)
            {
                CancelCurrentBooster();
            }
            else
            {
                LevelUI.DisableBoosterUI();
            }
            return result;
        }
        #endregion
    }
}