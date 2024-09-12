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
            base.SetupLevel();
        }
        
        public override void ResetLevelState()
        {
            base.ResetLevelState();
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

        public virtual void OnStageCompleted(int stageIndex)
        {
            LevelUI.OnStageCompleted(stageIndex);
            foreach (var component in LevelComponents)
            {
                if (component is PuzzleLevelElement puzzleLevelElement)
                {
                    puzzleLevelElement.OnStageCompleted();
                }
            }
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
            // foreach(var elementStatePair in newState.LevelElementStates)
            // {
            //     if(!LevelElements.ContainsKey(elementStatePair.Key)) continue;
            //     LevelElements[elementStatePair.Key].LoadElementState(elementStatePair.Value);
            // }
            return true;
        }

        public Dictionary<int, byte[]> GetLevelElementsState()
        {
            Dictionary<int, byte[]> elementsState = new Dictionary<int, byte[]>();
            // //Get elements state
            // foreach(var pair in LevelElements)
            // {
            //     PuzzleLevelElementState levelElementState = pair.Value.GetElementState();
            //     elementsState[pair.Key] = Helpers.Serialize(levelElementState);
            // }
            return elementsState;
        }
        #endregion
        
        #region Boosters
        public virtual bool ActivateBooster(PuzzleBooster booster)
        {
            if (CurrentState != LevelState.Playing || !booster.CanBeBought()) return false;
            if (CurrentBooster != null)
            {
                CancelCurrentBooster();
            }
            CurrentBooster = booster;
            CurrentBooster.ActivateBooster(this);
            LevelUI.SetUIForBooster(booster);
            return true;
        }
        
        /// <summary>
        /// Cancels the current booster without buying it
        /// </summary>
        public virtual void CancelCurrentBooster()
        {
           DeactivateCurrentBooster();
        }
        
        /// <summary>
        /// Called after applying the booster effect succesfully
        /// </summary>
        /// <returns></returns>
        public bool CompleteBooster()
        {
            if (CurrentBooster == null) return false;
            bool result = CurrentBooster.CompleteBooster();
            DeactivateCurrentBooster();
            return result;
        }
        
        /// <summary>
        /// Deactivates the current booster
        /// </summary>
        private void DeactivateCurrentBooster()
        {
            if(LevelUI != null) LevelUI.DisableBoosterUI();
            CurrentBooster = null;
        }
        #endregion
    }
}