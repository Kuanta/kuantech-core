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

        [Header("Level Design")] 
        public LevelDesignAsset LevelDesignAsset;
        
        [Header("Tutorial")]
        public int TutorialIndex = -1;

        //Boosters
        [NonSerialized] public PuzzleBooster CurrentBooster;

        //State
        [NonSerialized] public PuzzleLevelState CurrentLevelState;

        public override void OnLevelSet()
        {
            ClearLevelState();
        }
        public override void SetupLevel()
        {
            CreateWinConditionTracker();
            LevelUI = PuzzleUIManager.GetLevelUI(); 
            if(LevelUI != null) LevelUI.OnLevelSetup(this);
            ResetUI();
            GetStateModel()?.SetCurrentLevel(this);
            base.SetupLevel();
        }

        protected override void PlayLevel()
        {
            base.PlayLevel();
            if(LevelUI != null) LevelUI.OnPlayLevel();
        }
        public virtual bool IsHardLevel()
        {
            return false;
        }

        public virtual bool IsBonusLevel()
        {
            return false;
        }
        
        /// <summary>
        /// Finds the level design data. If succesfful, loads it to level
        /// </summary>
        public virtual void FindLevelDesign()
        {
            LevelDesignData data = LevelDesignManager.GetLevelDesignData(LevelNumber);
            if (data == null && LevelDesignAsset != null)
            {
                //If data is null, try to read it from level design asset
                data = new LevelDesignData();
                data.CreateFromDesignAsset(LevelDesignAsset);
            }
            if (data == null) return;
            LoadLevelDesign(data);
        }

        protected virtual void LoadLevelDesign(LevelDesignData designData)
        {
            if (designData == null) return;
        }
        
        public override void ResetLevelState()
        {
            base.ResetLevelState();
            ClearLevelState();
            if(WinConditionTracker != null) WinConditionTracker.Reset(false);
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

        public int GetCurrentStage()
        {
            if (WinConditionTracker == null) return 0;
            return WinConditionTracker.GetCurrentStageIndex();
        }

        public void SetStage(int stageIndex)
        {
            if (WinConditionTracker == null) return;
            WinConditionTracker.GoToStage(stageIndex);
        }
        public virtual void CreateWinConditionTracker()
        {
            WinConditionTracker = new WinConditionTracker();
            if (!StageEntries.IsNullOrEmpty())
            {
                WinConditionTracker.SetStages(StageEntries);
            }
            WinConditionTracker.OnStageCompleted += OnStageCompleted;
            WinConditionTracker.OnNewStage += OnNewStage;
            WinConditionTracker.OnAllStagesCompleted += OnStagesCompleted;
        }

        public virtual void OnStageCompleted(int completedStageIndex)
        {
            LevelUI.OnStageCompleted(completedStageIndex);
        }
    
        public virtual void OnNewStage(int newStageIndex)
        {
            LevelUI.OnNewStage(newStageIndex);
            foreach (var component in LevelElements)
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
            UpdateLevelState();
            return CurrentLevelState;
        }

        public virtual bool LoadLevelState(PuzzleLevelState newState)
        {
            if (newState.LevelElementStates != null)
            {
                for (int i = 0; i < LevelElements.Count; ++i)
                {
                    LevelElement element = LevelElements[i];
                    if (!newState.LevelElementStates.ContainsKey(element.ElementId)) continue;
                    element.LoadElementState(newState.LevelElementStates[element.ElementId]);
                }
            }
            return true;
        }

        public virtual void UpdateLevelState()
        {
            CurrentLevelState = new PuzzleLevelState();
            CurrentLevelState.LevelElementStates = GetLevelElementsState();
        }
        public Dictionary<int, byte[]> GetLevelElementsState()
        {
            Dictionary<int, byte[]> elementsState = new Dictionary<int, byte[]>();
            //Get elements state
            foreach(var levelElement in LevelElements)
            {
                byte[] elementState = levelElement.GetElementState();
                if (elementState == null) continue;
                if (elementsState.ContainsKey(levelElement.ElementId))
                {
                    Debug.LogError($"Duplicate key for {levelElement.gameObject}");
                    continue;
                }
                elementsState[levelElement.ElementId] = elementState;
            }
            return elementsState;
        }

        protected virtual void ClearLevelState()
        {
            PuzzleStateModel stateModel = GetStateModel();
            if (stateModel == null) return;
            stateModel.LevelState = null;
            CurrentLevelState = null;
            stateModel.Dirtied = true;
        }
        public virtual void DirtyLevelState()
        {
            PuzzleStateModel stateModel = GetStateModel();
            if (stateModel == null) return;
            stateModel.Dirtied = true;
            // if (CurrentState != LevelState.Playing)
            // {
            //     CurrentLevelState = null;
            // }
            // else
            // {
            //     UpdateLevelState();
            // }
        }

        public virtual PuzzleStateModel GetStateModel()
        {
            PuzzleStateModel stateModel = GameStateManager.GetModuleStatic<PuzzleStateModel>();
            return stateModel;
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